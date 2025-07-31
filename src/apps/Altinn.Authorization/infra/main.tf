terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.37.0"
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
  features {}
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

resource "azurerm_resource_group" "authorization" {
  name     = "rg${local.suffix}"
  location = "norwayeast"
  tags     = merge({}, local.default_tags)
}

resource "azurerm_user_assigned_identity" "authorization" {
  name                = "mi${local.suffix}"
  location            = azurerm_resource_group.authorization.location
  resource_group_name = azurerm_resource_group.authorization.name
  tags                = merge({}, local.default_tags)
}

# resource "azurerm_federated_identity_credential" "aks_federation" {
#   name                = "Aks"
#   resource_group_name = azurerm_resource_group.authorization.name
#   parent_id           = azurerm_user_assigned_identity.authorization.id

#   audience = ["api://AzureADTokenExchange"]
#   subject  = "system:serviceaccount:${each.value.namespace}:${each.value.service_account}"
#   issuer   = each.value.issuer_url

#   for_each = { for federation in var.aks_federation : federation.issuer_url => federation }
# }

module "rbac" {
  source       = "../../../../infra/modules/rbac"
  principal_id = azurerm_user_assigned_identity.authorization.principal_id
  hub_suffix   = local.hub_suffix
  spoke_suffix = local.spoke_suffix

  use_app_configuration = true
  use_lease             = false
  use_masstransit       = false
  providers = {
    azurerm.hub = azurerm.hub
  }
}

module "rbac_platform_app" {
  source       = "../../../../infra/modules/rbac"
  principal_id = each.value
  hub_suffix   = local.hub_suffix
  spoke_suffix = local.spoke_suffix

  use_app_configuration = true
  use_lease             = false
  use_masstransit       = false
  providers = {
    azurerm.hub = azurerm.hub
  }

  for_each = toset(var.platform_workflow_principal_ids)
}

module "appsettings" {
  source     = "../../../../infra/modules/appsettings"
  hub_suffix = local.hub_suffix
  providers = {
    azurerm.hub = azurerm.hub
  }
}
