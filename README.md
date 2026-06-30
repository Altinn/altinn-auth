<div align="center">

| Project               | Quality Gate                                                                                                                                                                                                    | Bugs                                                                                                                                                                                     | Code Smells                                                                                                                                                                                            | Coverage                                                                                                                                                                                         | Duplicated Lines (%)                                                                                                                                                                                                         |
| --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **[Access Management](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement&branch=main)** | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement&branch=main) | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement&branch=main) | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement&branch=main) | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement&branch=main) | [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement&branch=main) |
| **[Authorization](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization&branch=main)**     | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization&branch=main)       | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization&branch=main)       | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization&branch=main)       | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization&branch=main)       | [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization&branch=main)       |

_SonarCloud quality metrics for `main`, refreshed by the [nightly scan](docs/SONARCLOUD.md)._

</div>

# Altinn Authorization

Authorization and access management for the Altinn 3 platform. This repository
holds the Policy Decision Point (PDP) and Policy Enforcement Point (PEP) that
authorize users, systems, and organizations accessing the platform, together with
the Access Management services that administer rights and delegations for apps,
resources, and API schemes.

## Repository structure

| Path | Contents |
| --- | --- |
| `src/apps/Altinn.Authorization` | Authorization app: access control (PDP) and policy enforcement (PEP). |
| `src/apps/Altinn.AccessManagement` | Access Management app: rights and delegation administration. |
| `src/apps/Altinn.Register` | Vendored copy of Register, maintained in [altinn-register](https://github.com/Altinn/altinn-register). |
| `src/libs` | Shared libraries (`Api.Contracts`, `Host`, `Integration`). |
| `src/pkgs` | Published NuGet packages (`Altinn.Authorization.ABAC`, `Altinn.Common.PEP`). |
| `src/tools` | The `Altinn.Authorization.Cli` command-line tool. |
| `docs` | Documentation (see [Documentation](#documentation)). |
| `eng` | Build, test, and coverage scripts. |
| `infra` | Infrastructure as code. |

## Getting started

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- A container runtime, [Docker](https://www.docker.com/get-docker) or [Podman](https://podman.io), for integration tests and local services
- [PowerShell 7+](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
- [Just](https://github.com/casey/just) for the local-development commands

### Build and test

```bash
dotnet build Altinn.Authorization.sln
dotnet test                                          # unit + integration
dotnet test -- --filter-trait "Category=Unit"        # unit tests only
```

Integration tests need a running container runtime. See the
[testing guide](docs/testing/README.md) for the full picture.

### Run an app

```bash
dotnet run --project src/apps/Altinn.Authorization/src/Altinn.Authorization
dotnet run --project src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement
```

Each app exposes a Swagger UI on startup. Running against a real database needs
the local-development setup below.

## Local Development Environment

Additional tooling for working against Azure-backed dependencies:
[Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli),
[kubectl](https://kubernetes.io/docs/tasks/tools/),
[kubelogin](https://azure.github.io/kubelogin/install.html).

### 1. Sign in to Azure

Sign in with your `ai-dev` or `ai-prod` account so the test-tool secrets can be
fetched:

```bash
az login
```

### 2. Start dependencies and configure secrets

Replace `<subscription-id>` and `<key-vault-name>` with the values for your Azure
environment.

```bash
just dev   # starts PostgreSQL and supporting services via the container runtime

dotnet user-secrets set "PostgreSQLSettings:AdminConnectionString" $(just dev-pgsql-connection-string) --id Altinn.Authorization
dotnet user-secrets set "PostgreSQLSettings:AuthorizationDbAdminPwd" admin --id Altinn.Authorization
dotnet user-secrets set "PostgreSQLSettings:ConnectionString" $(just dev-pgsql-connection-string) --id Altinn.Authorization
dotnet user-secrets set "PostgreSQLSettings:AuthorizationDbPwd" admin --id Altinn.Authorization

az account set --subscription <subscription-id>

dotnet user-secrets set "Platform:Token:TestTool:Endpoint" $(az keyvault secret show --id=https://<key-vault-name>.vault.azure.net/secrets/Platform--Token--TestTool--Endpoint --query value --output tsv) --id Altinn.Authorization
dotnet user-secrets set "Platform:Token:TestTool:Password" $(az keyvault secret show --id=https://<key-vault-name>.vault.azure.net/secrets/Platform--Token--TestTool--Password --query value --output tsv) --id Altinn.Authorization
dotnet user-secrets set "Platform:Token:TestTool:Username" $(az keyvault secret show --id=https://<key-vault-name>.vault.azure.net/secrets/Platform--Token--TestTool--Username --query value --output tsv) --id Altinn.Authorization
```

### 3. Bootstrap the database

Open [`http://localhost:8000`](http://localhost:8000) and log in with
`admin@admin.com` / `admin`, then:

1. Create the `authorizationdb` database.
2. Create two login roles with privileges on it:
   - `platform_authorization` (`can_login`)
   - `platform_authorization_admin` (`can_login`, `superuser`)

## Documentation

- [Testing guide](docs/testing/README.md): how the test suite is organised and run.
- [SonarCloud](docs/SONARCLOUD.md): static analysis, exclusions, and the quality gate.

## Contributing

Open a pull request against `main`. CI builds and tests the verticals affected
by your change.

## Security

Report security vulnerabilities as described in [SECURITY.md](SECURITY.md).

## License

Licensed under the [MIT License](LICENSE).
