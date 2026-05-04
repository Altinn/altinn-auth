# Set shell for non-Windows OSs:
set shell := ["pwsh", "-c"]

# Set shell for Windows OSs:
set windows-shell := ["pwsh.exe", "-NoLogo", "-Command"]

set dotenv-load := true

[private]
@default:
  just --choose

@register-export-errors ENV='at22':
  dotnet run --project './Altinn.Authorization.Cli/src/Altinn.Authorization.Cli' --no-launch-profile -- sb export-errors '${REGISTER_SB_{{uppercase(ENV)}}}'

@register-retry-errors ENV='at22':
  dotnet run --project './Altinn.Authorization.Cli/src/Altinn.Authorization.Cli' --no-launch-profile -- register retry '${REGISTER_SB_{{uppercase(ENV)}}}' '${REGISTER_DB_{{uppercase(ENV)}}}'
