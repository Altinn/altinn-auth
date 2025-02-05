terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.13.0"
    }
    static = {
      source  = "tiwood/static"
      version = "0.1.0"
    }
  }

  backend "azurerm" {
    use_azuread_auth = true
  }
}

provider "azurerm" {
  features {
  }
}

provider "azurerm" {
  alias           = "hub"
  subscription_id = var.hub_subscription_id
  features {
  }
}

locals {
  hub_suffix                = lower("${var.organization}${var.product_name}${var.instance}hub")
  hub_resource_group_name   = lower("rg${local.hub_suffix}")
  spoke_suffix              = lower("${var.organization}${var.product_name}${var.instance}${var.environment}")
  spoke_resource_group_name = lower("rg${local.spoke_suffix}")
  suffix                    = lower("${var.organization}${var.product_name}${var.name}${var.instance}${var.environment}")

  default_tags = {
    ProductName = var.product_name
    Environment = var.environment
    Instance    = "001"
    Name        = var.name
    CreatedAt   = try(static_data.static.output.created_at, timestamp())
  }
}

resource "static_data" "static" {
  data = {
    created_at = formatdate("EEEE, DD-MMM-YY hh:mm:ss ZZZ", "2018-01-02T23:12:01Z")
  }

  lifecycle {
    ignore_changes = [data]
  }
}

data "azurerm_private_dns_zone" "postgres" {
  name                = "privatelink.postgres.database.azure.com"
  resource_group_name = "rg${local.hub_suffix}"
  provider            = azurerm.hub
}

data "azurerm_subnet" "postgres" {
  name                 = "Postgres"
  virtual_network_name = "vnetss${local.spoke_suffix}"
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

resource "azurerm_federated_identity_credential" "aks_federation" {
  name                = "Aks"
  resource_group_name = azurerm_resource_group.access_management.name
  parent_id           = azurerm_user_assigned_identity.access_management.id

  audience = ["api://AzureADTokenExchange"]
  subject  = "system:serviceaccount:${each.value.namespace}:${each.value.service_account}"
  issuer   = each.value.issuer_url

  for_each = { for federation in var.aks_federation : federation.issuer_url => federation }
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
  providers = {
    azurerm.hub = azurerm.hub
  }
}

module "postgres_server" {
  source              = "../../../../infra/modules/postgres"
  suffix              = local.suffix
  resource_group_name = azurerm_resource_group.register.name
  location            = "norwayeast"

  subnet_id           = data.azurerm_subnet.postgres.id
  private_dns_zone_id = data.azurerm_private_dns_zone.postgres.id
  postgres_version    = "17"
  configurations = {
    "azure.extensions" : "HSTORE"
  }

  compute_tier = "Burstable"
  compute_size = "Standard_B1ms"

  entraid_admins = [
    {
      principal_id   = data.azurerm_user_assigned_identity.admin.principal_id
      principal_name = data.azurerm_user_assigned_identity.admin.name
      principal_type = "ServicePrincipal"
    }
  ]
}
