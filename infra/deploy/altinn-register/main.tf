terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.14.0"
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
  features {}
}

provider "azurerm" {
  alias           = "hub"
  subscription_id = var.hub_subscription_id
  features {
  }
}

data "azurerm_client_config" "current" {}

locals {
  environment = lower(var.environment)
  suffix      = "register${var.organization}${var.product_name}${var.instance}${var.environment}"
  default_tags = {
    Component   = "Register"
    ProductName = var.product_name
    Environment = var.environment
    Instance    = "001"
    CreatedAt   = try(static_data.static.output.created_at, formatdate("EEEE, DD-MMM-YY hh:mm:ss ZZZ", "2018-01-02T23:12:01Z"))
  }

  hub_suffix              = lower("${var.organization}${var.product_name}${var.instance}hub")
  hub_resource_group_name = lower("rg${local.hub_suffix}")

  spoke_suffix              = lower("${var.organization}${var.product_name}${var.instance}${var.environment}")
  spoke_resource_group_name = lower("rg${local.spoke_suffix}")
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

resource "azurerm_resource_group" "register" {
  name     = "rgregister${local.suffix}"
  location = "norwayeast"

  lifecycle {
    prevent_destroy = true
  }
}

module "postgres_server" {
  source              = "../../modules/postgres"
  name                = "register"
  suffix              = local.suffix
  resource_group_name = azurerm_resource_group.register.name
  location            = "norwayeast"

  subnet_id           = data.azurerm_subnet.postgres.id
  private_dns_zone_id = data.azurerm_private_dns_zone.postgres.id

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

resource "azurerm_management_lock" "delete" {
  name       = "Terraform"
  scope      = each.key
  lock_level = "CanNotDelete"
  notes      = "Terraform Managed Lock"

  for_each = { for lock in [
    module.postgres_server
  ] : lock.name => lock.id }
}
