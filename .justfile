# Cross platform shebang:
shebang := if os() == 'windows' { 'pwsh.exe' } else { '/usr/bin/env pwsh'}

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
@update-sln-files *ARGS: install-script-packages-frozen
  #!{{shebang}}
  node ./.github/scripts/update-sln-files.mts -- {{ARGS}}

# Print all projects metadata
@get-metadata: install-script-packages-frozen
  #!{{shebang}}
  node ./.github/scripts/get-metadata.mts

# Print DB username and password for Entra ID auth for Azure postgres Flex Servers
db-cred:
  #!{{shebang}}
  dotnet run --project "./src/tools/Altinn.Authorization.Cli/src/Altinn.Authorization.Cli" -- db cred

# Dispatches a set of containers that's used for local dev
dev:
  #!{{shebang}}
  {{container-tool}} compose up -d

# Print connection string (accessmgmt db)
dev-pgsql-connection-string:
  #!{{shebang}}
  $port = {{container-tool}} inspect --format='{{"{{(index .NetworkSettings.Ports \"5432/tcp\" 0).HostPort}}"}}' altinn_authorization_postgres
  if ($IsWindows) {
    Write-Output "Host=host.containers.internal;Port=$port;Username=admin;Password=admin;Database=accessmgmt"
  } else {
    $bridge_ip = ip a | grep docker0 | awk '/inet / {print $2}' | cut -d'/' -f1
    Write-Output "Host=$bridge_ip;Port=$port;Username=admin;Password=admin;Database=accessmgmt"
  }

# Starts redis shell connected to docker composer redis instance
dev-redis-cli:
  #!{{shebang}}
  $port = {{container-tool}} inspect --format='{{"{{(index .NetworkSettings.Ports \"6379/tcp\" 0).HostPort}}"}}' altinn_authorization_redis
  redis-cli -h localhost -p $port
