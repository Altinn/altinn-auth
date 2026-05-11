# Set shell for non-Windows OSs:
set shell := ["pwsh", "-c"]

# Set shell for Windows OSs:
set windows-shell := ["pwsh.exe", "-NoLogo", "-Command"]

set dotenv-load := true

cli := "dotnet run --project './Altinn.Authorization.Cli/src/Altinn.Authorization.Cli' --no-launch-profile --"

[private]
@default:
  just --choose

@register-export-errors ENV='at22':
  {{cli}} sb export-errors '${REGISTER_SB_{{uppercase(ENV)}}}'

@register-retry-errors ENV='at22':
  {{cli}} register retry '${REGISTER_SB_{{uppercase(ENV)}}}' '${REGISTER_DB_{{uppercase(ENV)}}}'

@proxy ENV='at22' PORT='4028':
  {{cli}} proxy \
    --port {{PORT}} \
    --maskinporten-endpoint '${MASKINPORTEN_{{uppercase(ENV)}}_ENDPOINT}' \
    --maskinporten-client-id '${MASKINPORTEN_{{uppercase(ENV)}}_CLIENT_ID}' \
    --maskinporten-scope 'folkeregister:deling/offentligmedhjemmel skatteetaten:skatteetatenregistrertselskap' \
    --maskinporten-key '${MASKINPORTEN_{{uppercase(ENV)}}_KEY}' \
    --sire '${SIRE_{{uppercase(ENV)}}_ENDPOINT}' \
    --sire-events '${SIRE_EVENTS_{{uppercase(ENV)}}_ENDPOINT}' \
    --freg '${FREG_{{uppercase(ENV)}}_ENDPOINT}'
