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

data "azurerm_client_config" "current" {}

locals {
  repository          = "github.com/altinn/altinn-authorization-tmp/infra/deploy/cert"
  environment         = lower(var.environment)
  name                = "cert"
  resource_group_name = "rg${local.metadata.suffix}"

  metadata = {
    name        = local.name
    environment = local.environment
    instance    = var.instance
    suffix      = "${local.name}${var.instance}${var.environment}"
    repository  = local.repository
  }
}

resource "azurerm_resource_group" "cert" {
  name     = local.resource_group_name
  location = var.location

  tags = local.metadata
  lifecycle {
    prevent_destroy = true
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/key_vault
resource "azurerm_key_vault" "key_vault" {
  name                = "kvaltinnauth${local.metadata.suffix}"
  resource_group_name = azurerm_resource_group.cert.name
  location            = var.location
  tenant_id           = var.tenant_id

  sku_name                  = "standard"
  enable_rbac_authorization = true
  purge_protection_enabled  = true

  soft_delete_retention_days    = 30
  public_network_access_enabled = true

  network_acls {
    bypass         = "AzureServices"
    default_action = "Allow"
  }

  tags = local.metadata

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags]
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/user_assigned_identity
resource "azurerm_user_assigned_identity" "identity" {
  name                = "mi${local.metadata.suffix}"
  resource_group_name = azurerm_key_vault.key_vault.resource_group_name
  location            = var.location

  tags = local.metadata

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags]
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "mi" {
  scope                = azurerm_key_vault.key_vault.id
  principal_id         = azurerm_user_assigned_identity.identity.principal_id
  role_definition_name = "Key Vault Administrator" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "appreg" {
  scope                = azurerm_key_vault.key_vault.id
  principal_id         = data.azurerm_client_config.current.object_id
  role_definition_name = "Key Vault Administrator" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security
}
