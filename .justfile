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
