# Cross platform shebang:
shebang := if os() == 'windows' {
  'pwsh.exe'
} else {
  '/usr/bin/env pwsh'
}

# Set shell for non-Windows OSs:
set shell := ["pwsh", "-CommandWithArgs"]

# Set shell for Windows OSs:
set windows-shell := ["pwsh.exe", "-NoLogo", "-CommandWithArgs"]

[private]
@default:
  just --list

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
  ./.github/scripts/node_modules/.bin/tsx ./.github/scripts/update-sln-files.mts -- {{ARGS}}

# Print all projects metadata
@get-metadata: install-script-packages-frozen
  #!{{shebang}}
  ./.github/scripts/node_modules/.bin/tsx ./.github/scripts/get-metadata.mts

@db-cred:
  dotnet run --project "./src/tools/Altinn.Authorization.Cli/src/Altinn.Authorization.Cli" -- db cred
