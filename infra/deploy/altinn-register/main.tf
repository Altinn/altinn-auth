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

provider "azurerm" {
  use_oidc = true
  features {}
}

data "azurerm_client_config" "current" {}

locals {
  repository          = "github.com/altinn/altinn-authorization"
  environment         = lower(var.environment)
  name                = "register"
  resource_group_name = "rg${local.metadata.suffix}"

  infrastructure_suffix              = "${var.infrastructure_name}${var.instance}${var.environment}"
  infrastructure_resource_group_name = "rg${local.infrastructure_suffix}"

  metadata = {
    name        = local.name
    environment = local.environment
    instance    = var.instance
    suffix      = "${local.name}${var.instance}${var.environment}"
    repository  = local.repository
  }
}

data "azurerm_subnet" "postgres" {
  name                 = "postgres"
  resource_group_name  = local.infrastructure_resource_group_name
  virtual_network_name = "vnet${local.infrastructure_suffix}"
}

data "azurerm_subnet" "default" {
  name                 = "default"
  resource_group_name  = local.infrastructure_resource_group_name
  virtual_network_name = "vnet${local.infrastructure_suffix}"
}

data "azurerm_servicebus_namespace" "sb" {
  name                = "sbaltinn${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_private_dns_zone" "postgres" {
  name                = "privatelink.postgres.database.azure.com"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_private_dns_zone" "key_vault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_user_assigned_identity" "admin" {
  name                = "miappadmin${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

resource "azurerm_resource_group" "rg" {
  name     = local.resource_group_name
  location = var.location

  tags = local.metadata
}

resource "azurerm_user_assigned_identity" "managed_identity" {
  name                = "mi${local.metadata.suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_role_assignment" "mass_transit_role" {
  principal_id                     = azurerm_user_assigned_identity.managed_identity.principal_id
  scope                            = data.azurerm_servicebus_namespace.sb.id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
  role_definition_name             = "Azure Service Bus Mass Transit ${upper(var.environment)}"
}

resource "azurerm_role_assignment" "key_vault_secret_reader" {
  principal_id                     = azurerm_user_assigned_identity.managed_identity.principal_id
  scope                            = module.key_vault.id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
  role_definition_name             = "Key Vault Secrets User"
}

module "key_vault" {
  source              = "../../modules/key_vault"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  metadata            = local.metadata
  entraid_admins      = { "app" : data.azurerm_user_assigned_identity.admin.principal_id }

  dns_zones = [data.azurerm_private_dns_zone.key_vault.id]
  subnet_id = data.azurerm_subnet.default.id
  tenant_id = data.azurerm_client_config.current.tenant_id
}

module "postgres_server" {
  source              = "../../modules/postgres_server"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  metadata            = local.metadata

  entraid_admins = [
    {
      principal_id   = data.azurerm_user_assigned_identity.admin.principal_id
      principal_name = data.azurerm_user_assigned_identity.admin.name
      principal_type = "ServicePrincipal"
    }
  ]

  is_prod_like = var.is_prod_like
  key_vault_id = module.key_vault.id
  dns_zone     = data.azurerm_private_dns_zone.postgres.id

  subnet_id = data.azurerm_subnet.postgres.id
  tenant_id = data.azurerm_client_config.current.tenant_id
}

