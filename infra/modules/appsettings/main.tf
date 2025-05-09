terraform {
  required_providers {
    azurerm = {
      source                = "hashicorp/azurerm"
      configuration_aliases = [azurerm.hub]
    }
  }
}

data "azurerm_client_config" "hub" {
  provider = azurerm.hub
}

locals {
  configuration_store_id = "/subscriptions/${data.azurerm_client_config.hub.subscription_id}/resourceGroups/rg${var.hub_suffix}/providers/Microsoft.AppConfiguration/configurationStores/appconf${var.hub_suffix}"
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
  enabled                = each.value.default
  label                  = each.value.label

  provider = azurerm.hub
  lifecycle {
    ignore_changes = [enabled]
  }

  for_each = { for flag in var.feature_flags : "${flag.name}_${flag.label}" => flag }
}

resource "azurerm_app_configuration_key" "key_value" {
  configuration_store_id = local.configuration_store_id
  type                   = "kv"
  key                    = each.value.key
  value                  = each.value.value
  label                  = each.value.label
  provider               = azurerm.hub

  for_each = { for flag in var.key_value : "${flag.key}_${flag.label}" => flag }
}

resource "azurerm_app_configuration_key" "key_vault_reference" {
  configuration_store_id = local.configuration_store_id
  type                   = "vault"
  key                    = each.value.key
  vault_key_reference    = each.value.key_vault_secret_id
  label                  = each.value.label
  provider               = azurerm.hub

  for_each = { for flag in var.key_vault_reference : "${flag.key}_${flag.label}" => flag }
}

resource "azurerm_app_configuration_key" "labels_values" {
  configuration_store_id = local.configuration_store_id
  type                   = "kv"
  key                    = each.value.key
  value                  = each.value.value
  label                  = each.value.label
  content_type           = each.value.content_type
  provider               = azurerm.hub

  for_each = {
    for value in flatten([
      for label, label_cfg in var.labels : [
        for key, key_cfg in label_cfg.values : {
          tf_key       = "${label}|${key}"
          key          = key
          label        = label
          value        = key_cfg.value
          content_type = key_cfg.content_type
        }
      ]
    ])
    : value.tf_key => value
  }
}

resource "azurerm_app_configuration_key" "labels_vault_references" {
  configuration_store_id = local.configuration_store_id
  type                   = "vault"
  key                    = each.value.key
  vault_key_reference    = each.value.vault_key_reference
  label                  = each.value.label
  provider               = azurerm.hub

  for_each = {
    for value in flatten([
      for label, label_cfg in var.labels : [
        for key, key_cfg in label_cfg.vault_references : {
          tf_key              = "${label}|${key}"
          key                 = key
          label               = label
          vault_key_reference = key_cfg.vault_key_reference
        }
      ]
    ])
    : value.tf_key => value
  }
}

resource "azurerm_app_configuration_feature" "labels_feature_flags" {
  configuration_store_id = local.configuration_store_id
  key                    = each.value.key
  name                   = each.value.name
  description            = each.value.description
  enabled                = false
  label                  = each.value.label
  provider               = azurerm.hub

  lifecycle {
    ignore_changes = [enabled]
  }

  for_each = {
    for value in flatten([
      for label, label_cfg in var.labels : [
        for key, key_cfg in label_cfg.feature_flags : {
          tf_key      = "${label}|${key}"
          key         = key
          label       = label
          name        = key_cfg.name == null ? key : key_cfg.name
          description = key_cfg.description
        }
      ]
    ])
    : value.tf_key => value
  }
}
