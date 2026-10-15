# AGENTS.md — Altinn.Mockporten

A synthetic-only OpenID Connect provider ("Test-IDP") that Altinn Authentication can use as an upstream identity provider in automated tests, also in production-like environments. It only ever issues tokens for synthetic (Tenor) fødselsnummer. Imported from altinn-authentication with its full history (#4058). Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md); the full description is in [README.md](README.md).

## Commands

```bash
dotnet build src/apps/Altinn.Mockporten/Altinn.Mockporten.slnx
dotnet test src/apps/Altinn.Mockporten/Altinn.Mockporten.slnx
dotnet run --project src/apps/Altinn.Mockporten/src/Mockporten   # appsettings.Development.json enables it locally
```

## Where things live

`src/Mockporten` is the service (`AuthorizeController`, token issuing, signing keys from Key Vault); `test/Mockporten.Tests` covers the synthetic-number gate and the shared-password validator.

## Landmines

- **The synthetic-only gate is load-bearing.** `NorwegianIdentityNumber.IsSyntheticTenorPid` must stay fail-closed: only month 81–92 with valid mod11 passes. Altinn Authentication enforces the same rule independently with `RequireSyntheticPid`. Read [SECURITY.md](SECURITY.md) before touching either.
- **The shared password is checked before the `pid`**, so the endpoint reveals nothing until the password is proven. Keep that order; it is what stops the service being used as an oracle.
- **`GeneralSettings:TestIdpEnabled` defaults to `false` in code** and is switched on per environment. Do not change the code default.
- **This vertical is a build island** with its own build files and an `.editorconfig` with `root = true`. Do not merge it into the shared build files.
