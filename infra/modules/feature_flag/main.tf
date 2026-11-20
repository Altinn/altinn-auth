terraform {
  required_providers {
    azurerm = {
      source                = "hashicorp/azurerm"
      configuration_aliases = [azurerm.hub]
    }
  }
}

locals {
  environments = ["at22", "at23", "at24", "yt01", "tt02", "prod"]
}

variable "configuration_store_id" {
  type        = string
  description = "Azure App configuration store id."
}

variable "app" {
  type        = string
  description = "name of the app which will become the label <environment>-<app>"
}

variable "name" {
  type        = string
  description = "feature flag name"
}

variable "description" {
  type    = string
  default = "feature flag description"
}

resource "azurerm_app_configuration_feature" "flags" {
  for_each = toset(local.environments)

  configuration_store_id = var.configuration_store_id
  key                    = var.name
  name                   = var.name
  description            = var.description
  enabled                = false
  label                  = "${each.key}-${var.app}"
  provider               = azurerm.hub

  lifecycle {
    ignore_changes = [enabled]
  }
}
