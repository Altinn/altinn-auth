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
  hub_suffix = lower("${var.organization}${var.product_name}${var.name}${var.instance}hub")
  suffix     = lower("${var.organization}${var.product_name}${var.name}${var.instance}${var.environment}")

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

data "azurerm_resource_group" "hub" {
  name     = "rg${local.hub_suffix}"
  provider = azurerm.hub
}

data "azurerm_app_configuration" "app_configuration" {
  name                = "appconf${local.hub_suffix}"
  resource_group_name = data.azurerm_resource_group.hub.name
  provider            = azurerm.hub
}

data "azurerm_storage_account" "storage_account" {
  name                = "st${local.suffix}"
  resource_group_name = azurerm_resource_group.access_management.name
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
}

# https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
resource "azurerm_role_assignment" "app_configuration_data_reader" {
  scope                            = data.azurerm_app_configuration.app_configuration.id
  principal_id                     = azurerm_user_assigned_identity.access_management
  skip_service_principal_aad_check = true
  role_definition_name             = "App Configuration Data Reader"
}

resource "azurerm_role_assignment" "storage_blob_data_contributor" {
  scope                            = data.azurerm_storage_account.storage_account.id
  principal_id                     = azurerm_user_assigned_identity.access_management
  skip_service_principal_aad_check = true
  role_definition_name             = "Storage Blob Data Contributor"
}

resource "azurerm_federated_identity_credential" "aks_federation" {
  name                = "Aks"
  resource_group_name = azurerm_resource_group.access_management.name
  parent_id           = azurerm_user_assigned_identity.access_management.id

  audience = ["api://AzureADTokenExchange"]
  subject  = "system:serviceaccount:${var.aks_federation.namespace}:${var.aks_federation.service_account}"
  issuer   = var.aks_federation.issuer_url
}

