terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.51.0"
    }
    static = {
      source  = "tiwood/static"
      version = "0.1.0"
    }
    time = {
      source  = "hashicorp/time"
      version = "0.13.1"
    }
  }

  backend "azurerm" {
    use_azuread_auth = true
  }
}

provider "azurerm" {
  features {}
}


provider "azurerm" {
  alias           = "hub"
  subscription_id = var.hub_subscription_id
  features {}
}

locals {
  hub_suffix                = lower("${var.organization}${var.product_name}${var.instance}hub")
  hub_resource_group_name   = lower("rg${local.hub_suffix}")
  spoke_suffix              = lower("${var.organization}${var.product_name}${var.instance}${var.environment}")
  spoke_resource_group_name = lower("rg${local.spoke_suffix}")
  suffix                    = lower("${var.organization}${var.product_name}${var.name}${var.instance}${var.environment}")
  conf_json                 = jsondecode(file(local.conf_json_path))
  conf_json_path            = abspath("${path.module}/../conf.json")

  default_tags = {
    ProductName = var.product_name
    Environment = var.environment
    Instance    = "001"
    Name        = var.name
    CreatedAt   = try(static_data.static.output.created_at, timestamp())
  }
}

data "azurerm_client_config" "current" {}

resource "static_data" "static" {
  data = {
    created_at = formatdate("EEEE, DD-MMM-YY hh:mm:ss ZZZ", "2018-01-02T23:12:01Z")
  }

  lifecycle {
    ignore_changes = [data]
  }
}

data "azurerm_subnet" "postgres" {
  name                 = "Postgres"
  virtual_network_name = "vnetss${local.spoke_suffix}"
  resource_group_name  = local.spoke_resource_group_name
}

data "azurerm_subnet" "default" {
  name                 = "Default"
  virtual_network_name = "vnetds${local.spoke_suffix}"
  resource_group_name  = local.spoke_resource_group_name
}

data "azurerm_user_assigned_identity" "admin" {
  name                = "mipgsqladmin${local.spoke_suffix}"
  resource_group_name = local.spoke_resource_group_name
}

resource "azurerm_resource_group" "access_management" {
  name     = "rg${local.suffix}"
  location = "norwayeast"
  tags     = merge({}, local.default_tags)
}

resource "azurerm_user_assigned_identity" "access_management" {
  name                = "mi${local.suffix}"
  location            = azurerm_resource_group.access_management.location
  resource_group_name = azurerm_resource_group.access_management.name
  tags                = merge({}, local.default_tags)
}

# resource "azurerm_federated_identity_credential" "aks_federation" {
#   name                = "Aks"
#   resource_group_name = azurerm_resource_group.access_management.name
#   parent_id           = azurerm_user_assigned_identity.access_management.id

#   audience = ["api://AzureADTokenExchange"]
#   subject  = "system:serviceaccount:${each.value.namespace}:${each.value.service_account}"
#   issuer   = each.value.issuer_url

#   for_each = { for federation in var.aks_federation : federation.issuer_url => federation }
# }

module "rbac_platform_app" {
  source       = "../../../../infra/modules/rbac"
  principal_id = each.value
  hub_suffix   = local.hub_suffix
  spoke_suffix = local.spoke_suffix

  use_app_configuration = true
  use_lease             = true
  use_masstransit       = true
  providers = {
    azurerm.hub = azurerm.hub
  }

  for_each = toset(var.platform_workflow_principal_ids)
}

module "key_vault" {
  source              = "../../../../infra/modules/key_vault"
  name                = "acm"
  hub_suffix          = local.hub_suffix
  hub_subscription_id = var.hub_subscription_id
  resource_group_name = azurerm_resource_group.access_management.name
  suffix              = local.spoke_suffix
  subnet_id           = data.azurerm_subnet.default.id
  key_vault_roles = concat([
    {
      operation_id         = "grant_pgsqlrv_administrator"
      principal_id         = data.azurerm_user_assigned_identity.admin.principal_id
      role_definition_name = "Key Vault Administrator"
    },
    {
      operation_id         = "grant_deploy_app_administrator"
      principal_id         = var.deploy_app_principal_id
      role_definition_name = "Key Vault Administrator"
    },
    {
      operation_id         = "grant_access_management_app_secret_user"
      principal_id         = azurerm_user_assigned_identity.access_management.principal_id
      role_definition_name = "Key Vault Secrets User"
    }
    ],
    [
      for principal_id in var.platform_workflow_principal_ids :
      {
        operation_id         = "grant_access_management_platform_app_secret_user_${principal_id}"
        principal_id         = principal_id
        role_definition_name = "Key Vault Secrets User"
      }
    ],
    [
      for user in var.db_admins_user_principal_ids :
      {
        operation_id         = "grant_access_management_platform_app_secret_user_${user.principal_id}"
        principal_id         = user.principal_id
        role_definition_name = "Key Vault Secrets User"
      }
  ])
}

module "rbac" {
  source       = "../../../../infra/modules/rbac"
  principal_id = azurerm_user_assigned_identity.access_management.principal_id
  hub_suffix   = local.hub_suffix
  spoke_suffix = local.spoke_suffix

  use_app_configuration = true
  use_lease             = true
  use_masstransit       = true
  providers = {
    azurerm.hub = azurerm.hub
  }
}

data "azurerm_key_vault_secret" "postgres_migration" {
  key_vault_id = module.key_vault.id
  name         = "db-${module.postgres_server.name}-${local.conf_json.database.prefix}-migrator"
  depends_on   = [null_resource.bootstrap_database]
}

data "azurerm_key_vault_secret" "postgres_app" {
  key_vault_id = module.key_vault.id
  name         = "db-${module.postgres_server.name}-${local.conf_json.database.prefix}-app"
  depends_on   = [null_resource.bootstrap_database]
}

module "appsettings" {
  source     = "../../../../infra/modules/appsettings"
  hub_suffix = local.hub_suffix
  key_vault_reference = [
    {
      key                 = "Database:Postgres:AppConnectionString"
      key_vault_secret_id = data.azurerm_key_vault_secret.postgres_app.versionless_id
      label               = "${var.environment}-access-management"
    },
    {
      key                 = "Database:Postgres:MigrationConnectionString"
      key_vault_secret_id = data.azurerm_key_vault_secret.postgres_migration.versionless_id
      label               = "${var.environment}-access-management"
    }
  ]

  feature_flags = [
    {
      name        = "AccessMgmt.Enduser.Controller.ClientDelegation"
      description = "Specifies Client Delegation should be enabled in enduser API."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessMgmt.Core.Services.AuthorizedParties.EfEnabled"
      description = "(EF) Specifies if the AuthorizedParty service re-write to run on mainly EF-services should be used in dependency injection. Will need recycle of pods to take effect."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessMgmt.Core.HostedServices.RegisterSync"
      description = "(EF) Specifies if the resource register data should streamed from resource register service to access management database."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessMgmt.Core.HostedServices.RegisterSync.Import"
      description = "(EF) Specifies if the register should do try to do the initial ingest of register data on init."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessMgmt.Core.HostedServices.ResourceRegistrySync"
      description = "(EF) Specifies if the register data should streamed from register service to access management database."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AllAltinnRoleSync"
      description = "Specifies if the Altinn II roles should streamed from SBLBridge service to access management database"
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AltinnClientRoleSync"
      description = "Specifies if the Altinn II roles should streamed from SBLBridge service to access management database"
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AltinnAdminRoleSync"
      description = "Specifies if the Altinn II roles should streamed from SBLBridge service to access management database"
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessManagement.HostedServices.ResourceRegistrySync"
      description = "Specifies if the resource register data should streamed from resource register service to access management database."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessManagement.HostedServices.RegisterSync"
      description = "Specifies if the register data should streamed from register service to access management database."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessManagement.MigrationDb"
      description = "Specifies if database should be migrated using custom framework."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessManagement.MigrationDbEf"
      description = "Specifies if database should be migrating using Entity Framework."
      label       = "${lower(var.environment)}-access-management"
      value       = false
    },
    {
      name        = "AccessManagement.Enduser.Connections"
      description = "Specifies if feature access connections are enabled for endusers."
      label       = "${lower(var.environment)}-access-management"
      value       = true
    },
    {
      name        = "AccessManagement.Internal.Connections"
      description = "Specifies if feature access connections are enabled for internal usage."
      label       = "${lower(var.environment)}-access-management"
      value       = true
    },
  ]
  providers = {
    azurerm.hub = azurerm.hub
  }
}

module "postgres_server" {
  source              = "../../../../infra/modules/postgres"
  suffix              = local.suffix
  resource_group_name = azurerm_resource_group.access_management.name
  location            = "norwayeast"

  hub_suffix = local.hub_suffix

  subnet_id        = data.azurerm_subnet.postgres.id
  postgres_version = "16"
  configurations = {
    "azure.extensions" : "HSTORE"
  }

  storage_tier = var.db_storage_tier
  compute_sku  = var.db_compute_sku

  entraid_admins = concat([
    {
      principal_id   = data.azurerm_user_assigned_identity.admin.principal_id
      principal_name = data.azurerm_user_assigned_identity.admin.name
      principal_type = "ServicePrincipal"
    },
  ], var.db_admins_user_principal_ids)

  providers = {
    azurerm.hub = azurerm.hub
  }
}

resource "null_resource" "bootstrap_database" {
  triggers = {
    ts = timestamp()
  }

  depends_on = [module.postgres_server]
  provisioner "local-exec" {
    working_dir = "../../../tools/Altinn.Authorization.Cli/src/Altinn.Authorization.Cli"
    command     = <<EOT
      dotnet run -- db bootstrap ${local.conf_json_path} \
        --max-pool-size=${var.db_max_pool_size} \
        --tenant-id=${data.azurerm_client_config.current.tenant_id} \
        --principal-name=${data.azurerm_user_assigned_identity.admin.name} \
        --server-resource-group=${azurerm_resource_group.access_management.name} \
        --server-subscription=${data.azurerm_client_config.current.subscription_id} \
        --server-name=${module.postgres_server.name} \
        --kv-resource-group=${azurerm_resource_group.access_management.name} \
        --kv-subscription=${data.azurerm_client_config.current.subscription_id} \
        --kv-name=${module.key_vault.name}
  EOT
  }
}
