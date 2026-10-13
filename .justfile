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

# Update solution files with RepoCtl
@update-sln-files *ARGS:
  repoctl solutions update {{ARGS}}

@dotnet-reference-trimmer:
  dotnet build -p:EnableReferenceTrimmer=true

# List vertical metadata with RepoCtl
@get-metadata:
  repoctl verticals list

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
# Quality gates (#4086), following .github/workflows/ci.yml. For each vertical
# CI builds in Release with -warnaserror and runs `repoctl vertical test`, which runs every test
# in the vertical in one go, with no unit or integration lanes. The repository
# as a whole is checked with `repoctl check`.
#
# `check <vertical>` is the fast local gate: build and the unit tests, with no
# container runtime needed. It is not what CI runs. `check-full <vertical>` is.
#
# Recipes take the vertical's path, such as src/apps/Altinn.Authorization.
# Resource Registry runs xUnit v2 without categories, so its "unit tests" are
# all of its tests and start PostgreSQL.
#
# CI also passes -bl and the TRX reporting options; those change artefacts
# rather than the result and are left out.
# ---------------------------------------------------------------------------

# Build everything, or one vertical
@build path=".":
  dotnet build {{quote(path)}} -c Release -warnaserror

# The unit tests of one vertical: fast, and no container runtime needed
@test-unit path:
  dotnet test {{quote(path)}} -c Release -- --filter-trait "Category=Unit" --ignore-exit-code 8

# Every test of one vertical, as CI runs them
@test path: require-container-runtime require-repoctl
  repoctl vertical test {{quote(path)}} -c Release

# Fail clearly when no container runtime is reachable, instead of reporting an empty green lane
@require-container-runtime:
  if (-not (Get-Command {{container-tool}} -ErrorAction SilentlyContinue)) { Write-Host "{{container-tool}} is not installed. Integration tests need a container runtime."; exit 1 }; & {{container-tool}} info *> $null; if ($LASTEXITCODE -ne 0) { Write-Host "{{container-tool}} is installed but not reachable. Start it, or run 'just dev'."; exit 1 }
  if ($IsWindows -and -not $env:DOCKER_HOST -and -not (Test-Path '\\.\pipe\docker_engine')) { Write-Host "{{container-tool}} is running, but Testcontainers cannot reach it: DOCKER_HOST is not set and the docker_engine pipe does not exist. See docs/testing/GETTING_STARTED.md."; exit 1 }

# Fail clearly when RepoCtl is missing or older than the version CI pins in .github/actions/init/action.yml
@require-repoctl:
  if (-not (Get-Command repoctl -ErrorAction SilentlyContinue)) { Write-Host "repoctl is not installed. Run: dotnet tool install --global Altinn.Authorization.RepoCtl.Cli"; exit 1 }
  $want = (Select-String -Path .github/actions/init/action.yml -Pattern 'depName=Altinn.Authorization.RepoCtl.Cli' -Context 0,1).Context.PostContext[0] -replace '.*default:\s*"?([0-9.]+)"?.*', '$1'; $have = (repoctl --version) -replace '[^0-9.].*$', ''; if ([version]$have -lt [version]$want) { Write-Host "repoctl $have is older than $want, which CI uses. Run: dotnet tool update --global Altinn.Authorization.RepoCtl.Cli --version $want"; exit 1 }

# The fast gate for a vertical: build and its unit tests. Not what CI runs.
@check path: (build path) (test-unit path)

# What CI runs for a vertical: build, then every test through RepoCtl
@check-full path: require-container-runtime require-repoctl (build path)
  repoctl vertical test {{quote(path)}} -c Release --no-build

# On Windows, CRLF files in the working tree fail its format step; see docs/testing/GETTING_STARTED.md.
# The repository check CI runs: dotnet format, generated .slnx files, release config
@repo-check: require-repoctl
  repoctl check
