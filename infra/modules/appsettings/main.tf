provider "azurerm" {
  alias           = "hub"
  subscription_id = var.hub_subscription_id
  features {
  }
}

locals {
  configuration_store_id = "/subscriptions/${var.hub_subscription_id}/resourceGroups/rg${var.hub_suffix}/providers/Microsoft.AppConfiguration/configurationStores/appconf${var.hub_suffix}"
}

data "azurerm_resource_group" "hub" {
  name     = "rg${var.hub_suffix}"
  provider = azurerm.hub
}


// Auth issue when looking up app conf
# data "azurerm_app_configuration" "app_configuration" {
#   name                = "appconf${var.hub_suffix}"
#   resource_group_name = data.azurerm_resource_group.hub.name
#   provider            = azurerm.hub
# }

resource "azurerm_app_configuration_feature" "configuration" {
  configuration_store_id = local.configuration_store_id
  key                    = each.value.name
  name                   = each.value.name
  description            = each.value.description
  enabled                = false
  label                  = each.value.label

  provider = azurerm.hub
  lifecycle {
    ignore_changes = [enabled]
  }

  for_each = { for flag in var.feature_flags : "${flag.name}_${flag.label}" => flag }
}

resource "azurerm_app_configuration_key" "key_value" {
  configuration_store_id = local.configuration_store_id
  key                    = each.key
  type                   = "kv"
  value                  = each.value.value
  provider               = azurerm.hub

  for_each = { for flag in var.key_vault_reference : "${flag.key}_${flag.label}" => flag }
}

resource "azurerm_app_configuration_key" "key_vault_reference" {
  configuration_store_id = local.configuration_store_id
  key                    = each.key
  type                   = "vault"
  vault_key_reference    = each.value.key_vault_secret_id
  provider               = azurerm.hub

  for_each = { for flag in var.key_vault_reference : "${flag.key}_${flag.label}" => flag }
}
