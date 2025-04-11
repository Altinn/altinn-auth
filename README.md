<div align="center">

| Project               | Quality Gate                                                                                                                                                                                                    | Bugs                                                                                                                                                                                     | Code Smells                                                                                                                                                                                            | Coverage                                                                                                                                                                                         | Duplicated Lines (%)                                                                                                                                                                                                         |
| --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Access Management** | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement) | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement) | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement) | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement) | [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Authorization_AccessManagement&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Authorization_AccessManagement) |
| **Authorization**     | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization)       | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization)       | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization)       | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization)       | [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Authorization&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Authorization_Authorization)       |
| **Register**          | [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Register&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Authorization_Register)                 | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Register&metric=bugs)](https://sonarcloud.io/summary/new_code?id=Authorization_Register)                 | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Register&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=Authorization_Register)                 | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Register&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Authorization_Register)                 | [![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Authorization_Register&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=Authorization_Register)                 |

</div>

# Authorization

## Local Development Environment

### Prerequisites

Ensure you have the following languages and tools installed before setting up your development environment.

#### Languages
- .NET 9.0 & 8.0
- TypeScript

#### Tools
- [Just](https://github.com/casey/just?tab=readme-ov-file#installation)
- [Docker Desktop Windows](http://docs.docker.com/desktop/setup/install/windows-install/)
- [Docker Engine Linux](https://docs.docker.com/engine/install/)
- [Docker Compose Linux](https://docs.docker.com/compose/install/)
- [Docker Compose Windows](https://podman-desktop.io/docs/compose/setting-up-compose)
- [Azure CLI (az)](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [kubelogin](https://azure.github.io/kubelogin/install.html)
- [powershell core](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.5)

### Setting Up the Environment

#### Authenticate with Azure
Before executing the setup commands, log in using Azure CLI with the appropriate user:

```bash
az login
```

Use your `ai-dev` or `ai-prod` user.

#### Configure Dependencies
Run the following commands to initialize the development environment:

```bash
just dev

# Set up PostgreSQL secrets

dotnet user-secrets set "PostgreSQLSettings:AdminConnectionString" $(just dev-pgsql-connection-string) --id Altinn.Authorization
dotnet user-secrets set "PostgreSQLSettings:AuthorizationDbAdminPwd" admin --id Altinn.Authorization
dotnet user-secrets set "PostgreSQLSettings:ConnectionString" $(just dev-pgsql-connection-string) --id Altinn.Authorization
dotnet user-secrets set "PostgreSQLSettings:AuthorizationDbPwd" admin --id Altinn.Authorization

# Set Azure subscription
az account set --subscription 45177a0a-d27e-490f-9f23-b4726de8ccc1

# Configure Platform Token Test Tool credentials
dotnet user-secrets set "Platform:Token:TestTool:Endpoint" $(az keyvault secret show --id=https://rgaltinnauth001local.vault.azure.net/secrets/Platform--Token--TestTool--Endpoint --query value --output tsv) --id Altinn.Authorization
dotnet user-secrets set "Platform:Token:TestTool:Password" $(az keyvault secret show --id=https://rgaltinnauth001local.vault.azure.net/secrets/Platform--Token--TestTool--Password --query value --output tsv) --id Altinn.Authorization
dotnet user-secrets set "Platform:Token:TestTool:Username" $(az keyvault secret show --id=https://rgaltinnauth001local.vault.azure.net/secrets/Platform--Token--TestTool--Username --query value --output tsv) --id Altinn.Authorization
```

### Bootstrap Access Management

1. Open [`http://localhost:8000`](http://localhost:8000) in a browser.
2. Log in using:
   - **Username:** `admin@admin.com`
   - **Password:** `admin`
3. Create the `accessmgmt` database and configure roles:
   - **Role:** `platform_authorization` (Privileges: `can_login`)
   - **Role:** `platform_authorization_admin` (Privileges: `can_login`, `superuser`)
