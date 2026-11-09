terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.81.0"
    }
    static = {
      source  = "tiwood/static"
      version = "0.1.0"
    }
    time = {
      source  = "hashicorp/time"
      version = "0.14.2"
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

module "appsettings" {
  source     = "../../../../infra/modules/appsettings"
  hub_suffix = local.hub_suffix

  labels = {
    "${var.environment}-access-management" = {
      values = merge({
        "Consent:EventsPageSize"                      = { value = tostring(var.configuration.consent.events_page_size) }
        "Core:Request:NotifyRequestApprovedInSeconds" = { value = tostring(var.configuration.core.request_notify_request_approved_in_seconds) } # Deprecated
        "Core:Request:NotifyRequestPendingInSeconds"  = { value = tostring(var.configuration.core.request_notify_request_pending_in_seconds) }  # Deprecated

        "Core:Notifications:RequestReviewedNotifyInSeconds"    = { value = tostring(var.configuration.core.notifications.request_reviewed_notify_in_seconds) }
        "Core:Notifications:RequestPendingNotifyInSeconds"     = { value = tostring(var.configuration.core.notifications.request_pending_notify_in_seconds) }
        "Core:Notifications:RightholderAddedNotifyInSeconds"   = { value = tostring(var.configuration.core.notifications.rightholder_added_notify_in_seconds) }
        "Core:Notifications:RightholderRemovedNotifyInSeconds" = { value = tostring(var.configuration.core.notifications.rightholder_removed_notify_in_seconds) }
        "Core:Notifications:AccessAddedNotifyInSeconds"        = { value = tostring(var.configuration.core.notifications.access_added_notify_in_seconds) }
        "Core:Notifications:AccessRemovedNotifyInSeconds"      = { value = tostring(var.configuration.core.notifications.access_removed_notify_in_seconds) }
        "Core:Notifications:InstanceAddedNotifyInSeconds"      = { value = tostring(var.configuration.core.notifications.instance_added_notify_in_seconds) }
        "Core:Notifications:InstanceRemovedNotifyInSeconds"    = { value = tostring(var.configuration.core.notifications.instance_removed_notify_in_seconds) }
        },
        {
          for kv in flatten([
            for orgnumber, packages in var.configuration.serviceowner_delegation.package_whitelist :
            [
              for i, package in packages :
              {
                key   = "ServiceOwnerDelegation:PackageWhitelist:${orgnumber}:${i}"
                value = package
              }
            ]
          ]) : kv.key => { value = kv.value }
      })
    }
  }

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
