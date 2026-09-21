# Altinn Access Management

Backend functionality for Access Management:
- Administration of rights for apps and resources
- Administration of rights for API schemes

## Getting started

Open `Altinn.AccessManagement.sln` and run the `Altinn.AccessManagement` project
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
