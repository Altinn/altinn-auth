# Cross platform shebang:
shebang := if os() == 'windows' { 'pwsh.exe' } else { '/usr/bin/env pwsh'}
amdir := 'src/apps/Altinn.AccessManagement/src'

# Define the container runtime based on OS
container-tool := if os() == 'windows' {
  'podman' 
} else {
  'docker'
}

# Set shell for non-Windows OSs:
set shell := ["pwsh", "-c"]

# Set shell for Windows OSs:
set windows-shell := ["pwsh.exe", "-NoLogo", "-Command"]

[private]
@default:
  just --choose

# Install node packages required to run scripts - uses pnpm to install the packages
[private]
@install-script-packages:
  #!{{shebang}}
  pushd .github/scripts
  pnpm install

[private]
@install-script-packages-frozen:
  #!{{shebang}}
  pushd .github/scripts
  pnpm install --frozen-lockfile

# Run the script to update solution files
# Run the script to update solution files
@update-sln-files *ARGS: install-script-packages-frozen
  #!{{shebang}}
  node --import "./.github/scripts/node_modules/tsx/dist/esm/index.mjs" "./.github/scripts/update-sln-files.mts" -- {{ARGS}}

@dotnet-reference-trimmer:
  dotnet build -p:EnableReferenceTrimmer=true

# Print all projects metadata
@get-metadata: install-script-packages-frozen
  #!{{shebang}}
  node ./.github/scripts/get-metadata.mts

# Print DB username and password for Entra ID auth for Azure postgres Flex Servers
db-cred:
  #!{{shebang}}
  dotnet run --project "./src/tools/Altinn.Authorization.Cli/src/Altinn.Authorization.Cli" -- db cred

am-db-migrate message:
  dotnet ef migrations add -p {{amdir}}/Altinn.AccessMgmt.PersistenceEF -s {{amdir}}/Altinn.AccessManagement {{message}}

am-db-migrate-remove:
  dotnet ef migrations remove -p {{amdir}}/Altinn.AccessMgmt.PersistenceEF -s {{amdir}}/Altinn.AccessManagement

am-db-update:
  dotnet ef database update -p {{amdir}}/Altinn.AccessMgmt.PersistenceEF -s {{amdir}}/Altinn.AccessManagement

# Dispatches a set of containers that's used for local dev
dev:
  #!{{shebang}}
  {{container-tool}} compose up -d

# Print connection string (accessmgmt db)
dev-pgsql-connection-string:
  #!{{shebang}}
  $port = {{container-tool}} inspect --format='{{"{{(index .NetworkSettings.Ports \"5432/tcp\" 0).HostPort}}"}}' altinn_authorization_postgres
  if ($IsWindows) {
    Write-Output "Host=host.containers.internal;Port=$port;Username=admin;Password=admin;Database=authorizationdb"
  } else {
    $bridge_ip = ip a | grep docker0 | awk '/inet / {print $2}' | cut -d'/' -f1
    Write-Output "Host=$bridge_ip;Port=$port;Username=admin;Password=admin;Database=authorizationdb"
  }

# Starts redis shell connected to docker composer redis instance
dev-redis-cli:
  #!{{shebang}}
  $port = {{container-tool}} inspect --format='{{"{{(index .NetworkSettings.Ports \"6379/tcp\" 0).HostPort}}"}}' altinn_authorization_redis
  redis-cli -h localhost -p $port
# ---------------------------------------------------------------------------
# Quality gates (#4086).
#
# `check <vertical>` is the fast gate: build plus the unit lane. It is not the
# whole pipeline — CI also runs the integration lane after a successful build —
# so a green `check` is a cheap early signal, not a guarantee. `check-full`
# adds the integration lane and is the one that mirrors CI.
#
# The lane recipes take a vertical rather than defaulting to the repository,
# because Altinn.ResourceRegistry runs xUnit v2 and carries no category traits:
# the trait filter does not exclude its tests, so a repository-wide "unit" run
# would start its PostgreSQL container. Run that vertical with
# `just test src/apps/Altinn.ResourceRegistry`.
#
# CI also passes --no-incremental, -bl and --results-directory; those change
# artefacts rather than the result and are left out.
# ---------------------------------------------------------------------------

# Build everything, or one vertical
@build path=".":
  dotnet build {{quote(path)}} -c Release

# The unit lane for one vertical, selected as CI selects it
@test-unit path:
  dotnet test {{quote(path)}} -c Release -- --filter-trait "Category=Unit" --ignore-exit-code 8

# The integration lane for one vertical
@test-integration path: require-container-runtime
  dotnet test {{quote(path)}} -c Release -p:TestLane=Integration -- --filter-trait "Category=Integration" --ignore-exit-code 8

# Every test under a path, including verticals that have no lanes
@test path=".": require-container-runtime
  dotnet test {{quote(path)}} -c Release

# Fail clearly when no container runtime is reachable, instead of reporting an empty green lane
@require-container-runtime:
  if (-not (Get-Command {{container-tool}} -ErrorAction SilentlyContinue)) { Write-Host "{{container-tool}} is not installed. Integration tests need a container runtime."; exit 1 }; & {{container-tool}} info *> $null; if ($LASTEXITCODE -ne 0) { Write-Host "{{container-tool}} is installed but not reachable. Start it, or run 'just dev'."; exit 1 }
  if ($IsWindows -and -not $env:DOCKER_HOST -and -not (Test-Path '\\.\pipe\docker_engine')) { Write-Host "{{container-tool}} is running, but Testcontainers cannot reach it: DOCKER_HOST is not set and the docker_engine pipe does not exist. See docs/testing/GETTING_STARTED.md."; exit 1 }

# The fast gate for a vertical: build and the unit lane. Not the whole pipeline.
@check path: (build path) (test-unit path)

# Build plus both lanes: what CI actually runs for a vertical
@check-full path: require-container-runtime (check path) (test-integration path)

# There is deliberately no `lint` recipe. CI enforces no formatting gate today,
# `dotnet format --verify-no-changes` fails on main with StyleCop violations, and
# it does not scope to the path it is given the way build and test do. Adding a
# gate that is red on a clean checkout would only teach people to ignore it.
# Deciding whether to adopt a formatter is separate work; see #4086.
