terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      configuration_aliases = [
        azurerm.hub,
      ]
    }
  }
}

data "azurerm_resource_group" "hub" {
  name     = "rg${var.hub_suffix}"
  provider = azurerm.hub
}

data "azurerm_app_configuration" "app_configuration" {
  name                = "appconf${var.hub_suffix}"
  resource_group_name = data.azurerm_resource_group.hub.name
  provider            = azurerm.hub
}

resource "azurerm_app_configuration_feature" "configuration" {
  configuration_store_id = data.azurerm_app_configuration.app_configuration.id
  key                    = each.key
  name                   = each.key
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
  configuration_store_id = data.azurerm_app_configuration.app_configuration.id
  key                    = each.key
  type                   = "kv"
  value                  = each.value.value
  provider               = azurerm.hub

  for_each = { for flag in var.key_vault_reference : "${flag.key}_${flag.label}" => flag }
}

resource "azurerm_app_configuration_key" "key_vault_reference" {
  configuration_store_id = data.azurerm_app_configuration.app_configuration.id
  key                    = each.key
  type                   = "vault"
  vault_key_reference    = each.value.key_vault_secret_id
  provider               = azurerm.hub

  for_each = { for flag in var.key_vault_reference : "${flag.key}_${flag.label}" => flag }
}
