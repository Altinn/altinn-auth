# Altinn Access Management

Backend functionality for Access Management:
- Administration of rights for apps and resources
- Administration of rights for API schemes

## Getting started

Open `Altinn.AccessManagement.slnx` and run the `Altinn.AccessManagement` project
(the browser opens the Swagger UI automatically).

Or from the command line:

```bash
dotnet run --project src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement
```

## Local development

Local setup — containerised PostgreSQL (`just dev`), user secrets, and the
database bootstrap — is in the
[repository README](../../../README.md#local-development-environment). Access
Management uses the `authorizationdb` database (roles `platform_authorization` /
`platform_authorization_admin`).

## Deployment manifests

`syncroot/` holds the Flux objects for each environment, and `manifests/` holds
what they deploy, in `pre-deploy/<env>` and `deploy/<env>`. The Flux
Kustomizations in `syncroot/base` point at `./fake`; each environment overlay
patches them by name to its own folders. A patch whose target name matches
nothing is skipped without an error, so when you rename a Kustomization in
`syncroot/base`, update the target names in every environment overlay, not just
at22. `.github/scripts/validate-manifests.sh` fails if a rendered syncroot
does not point at existing overlays for its own environment.
