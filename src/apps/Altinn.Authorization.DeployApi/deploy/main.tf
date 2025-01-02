terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.14.0"
    }
  }

  backend "azurerm" {
    use_azuread_auth = true
  }
}
provider "azurerm" {
  use_oidc = true
  features {}
}

locals {
  infrastructure_suffix              = "${var.infrastructure_name}${var.instance}${var.environment}"
  infrastructure_resource_group_name = "rg${local.infrastructure_suffix}"
}

data "azurerm_user_assigned_identity" "application_admin" {
  name                = "miappadmin${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

module "app" {
  source = "../../../../infra/modules/container_app_api"

  user_assigned_identities = [data.azurerm_user_assigned_identity.application_admin.id]
  variables = {
    "ManagedIdentity__ClientId" = data.azurerm_user_assigned_identity.application_admin.client_id
  }

  instance    = var.instance
  environment = var.environment
  name        = "deployapi"
  image       = var.image
}
