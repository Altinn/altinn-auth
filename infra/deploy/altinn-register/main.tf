terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.16.0"
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
  features {
  }
}

data "azurerm_client_config" "current" {}

locals {
  environment = lower(var.environment)
  suffix      = "register${var.organization}${var.product_name}${var.instance}${var.environment}"
  default_tags = {
    Component   = "Register"
    ProductName = var.product_name
    Environment = var.environment
    Instance    = "001"
    CreatedAt   = try(static_data.static.output.created_at, formatdate("EEEE, DD-MMM-YY hh:mm:ss ZZZ", "2018-01-02T23:12:01Z"))
  }

  hub_suffix              = lower("${var.organization}${var.product_name}${var.instance}hub")
  hub_resource_group_name = lower("rg${local.hub_suffix}")

  spoke_suffix              = lower("${var.organization}${var.product_name}${var.instance}${var.environment}")
  spoke_resource_group_name = lower("rg${local.spoke_suffix}")
}
