terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.1.0"
    }
  }

  backend "azurerm" {
    use_azuread_auth = true
  }
}

data "azurerm_client_config" "current" {}

locals {
  infrastructure_suffix              = "${var.infrastructure_name}${var.instance}${var.environment}"
  infrastructure_resource_group_name = "rg${local.infrastructure_suffix}"
}

provider "azurerm" {
  use_oidc = true
  features {}
}

module "app" {
  source = "../../../../infra/modules/container_app_api"

  instance    = var.instance
  environment = var.environment
  name        = "accesspackages"
  image       = var.image

  can_use_auth_service_bus       = true
  can_use_auth_app_configuration = true
  can_use_auth_key_vault         = true
}

data "azurerm_postgresql_flexible_server" "auth" {
  name                = "psqlsrvaltinn${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_key_vault" "auth" {
  name                = "kvaltinn${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_user_assigned_identity" "auth" {
  name                = "miappadmin${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}
