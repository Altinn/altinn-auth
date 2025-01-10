terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.13.0"
    }
  }
}

provider "azurerm" {
  subscription_id = "45177a0a-d27e-490f-9f23-b4726de8ccc1"
  features {
  }

  # Configuration options
}

data "azurerm_client_config" "current" {}

locals {
  hub_suffix       = lower("${var.organization}${var.product_name}${var.instance}hub")
  suffix           = lower("${var.organization}${var.product_name}${var.instance}${var.environment_group}spoke")
  ipv4_cidr_prefix = tonumber(split("/", var.ipv4_address_space)[1])
  ipv6_cidr_prefix = tonumber(split("/", var.ipv6_address_space)[1])
}

data "azurerm_virtual_network" "hub" {
  name                = "vnet${local.hub_suffix}"
  resource_group_name = "rg${local.hub_suffix}"
}

resource "azurerm_resource_group" "spoke" {
  name     = "rg${local.suffix}"
  location = "norwayeast"
}

resource "azurerm_virtual_network" "spoke_dual_stack" {
  name                = "vnet${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  address_space = [
    var.ipv4_address_space,
    var.ipv6_address_space,
  ]
}

resource "azurerm_virtual_network_peering" "hub-spoke1-peer" {
  name                = "${var.environment_group}-spoke"
  resource_group_name = azurerm_resource_group.spoke.name

  virtual_network_name      = data.azurerm_virtual_network.hub.name
  remote_virtual_network_id = azurerm_virtual_network.spoke.id

  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = true
  use_remote_gateways          = false

  depends_on = [azurerm_virtual_network.spoke1-vnet, azurerm_virtual_network.hub-vnet, azurerm_virtual_network_gateway.hub-vnet-gateway]
}
