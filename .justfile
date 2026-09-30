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
# Quality gates. These mirror what CI runs per vertical (#4086), so that a
# green `just check` locally is a real prediction of a green pipeline.
#
# Every recipe takes an optional path: the repository root by default, or a
# single vertical, e.g. `just check src/apps/Altinn.Authorization`.
#
# CI also passes --no-incremental, -bl and --results-directory. Those change
# artefacts and build hygiene, not the result, so they are left out here.
# ---------------------------------------------------------------------------

# Build everything, or one vertical
@build path=".":
  dotnet build {{path}} -c Release

# The unit lane, selected exactly as CI selects it
@test-unit path=".":
  # Altinn.ResourceRegistry runs xUnit v2 and carries no category traits, so this
  # filter does not exclude its tests. Run that vertical on its own instead:
  # `just test src/apps/Altinn.ResourceRegistry`.
  dotnet test {{path}} -c Release -- --filter-trait "Category=Unit" --ignore-exit-code 8

# The integration lane. Needs a container runtime; `just dev` starts one.
@test-integration path=".":
  dotnet test {{path}} -c Release -p:TestLane=Integration -- --filter-trait "Category=Integration" --ignore-exit-code 8

# Every test, both lanes and the verticals that have no lanes
@test path=".":
  dotnet test {{path}} -c Release

# What to run before asking for review: what CI will run on the fast path
@check path=".": (build path) (test-unit path)

# There is deliberately no `lint` recipe. CI enforces no formatting gate today,
# `dotnet format --verify-no-changes` fails on main with StyleCop violations, and
# it does not scope to the path it is given the way build and test do. Adding a
# gate that is red on a clean checkout would only teach people to ignore it.
# Deciding whether to adopt a formatter is separate work; see #4086.
