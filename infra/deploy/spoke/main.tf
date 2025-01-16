terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.13.0"
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
  features {
  }
}

provider "azurerm" {
  alias           = "hub"
  subscription_id = var.hub_subscription_id
  features {
  }
}

data "azurerm_client_config" "current" {}

locals {
  hub_suffix = lower("${var.organization}${var.product_name}${var.instance}hub")
  suffix     = "${var.organization}${var.product_name}${var.instance}${var.environment}"

  ipv4_cidr_prefix = tonumber(split("/", var.single_stack_ipv4_address_space)[1])
  ipv6_cidr_prefix = tonumber(split("/", var.dual_stack_ipv6_address_space)[1])
  ipv6_bits        = 64 - local.ipv6_cidr_prefix

  default_tags = {
    ProductName = var.product_name
    Environment = var.environment
    Instance    = "001"
    CreatedAt   = try(static_data.static.output.created_at, timestamp())
  }

  dual_stack_subnets = [
    {
      name         = "Default"
      include_ipv6 = true
      ipv4_bits    = 22 - local.ipv4_cidr_prefix
      create       = true
    },
    {
      name         = "Aks"
      include_ipv6 = true
      ipv4_bits    = 22 - local.ipv4_cidr_prefix
      create       = false
    },
    {
      name         = "ServiceBus"
      include_ipv6 = true
      ipv4_bits    = 25 - local.ipv4_cidr_prefix
      create       = true
    }
  ]

  single_stack_subnets = [
    {
      name      = "Postgres"
      ipv4_bits = 22 - local.ipv4_cidr_prefix
      create    = true
    }
  ]
}

resource "static_data" "static" {
  data = {
    api_id     = uuid()
    created_at = timestamp()
  }

  lifecycle {
    ignore_changes = [data]
  }
}

data "azurerm_virtual_network" "hub_vnet" {
  name                = "vnet${local.hub_suffix}"
  resource_group_name = "rg${local.hub_suffix}"
  provider            = azurerm.hub
}

resource "azurerm_resource_group" "spoke" {
  name     = "rg${local.suffix}"
  location = "norwayeast"

  tags = merge({}, local.default_tags)
}

resource "azurerm_virtual_network" "dual_stack" {
  name                = "vnetds${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  location            = azurerm_resource_group.spoke.location
  address_space = [
    var.dual_stack_ipv4_address_space,
    var.dual_stack_ipv6_address_space
  ]

  dns_servers = [var.firewall_private_ipv4]

  tags = merge(
    { for subnet in local.dual_stack_subnets : "${subnet.name}IPv4" => module.subnet_ipv4_dual_stack.networks[index(module.subnet_ipv4_dual_stack.networks.*.name, subnet.name)].cidr_block },
    { for subnet in local.dual_stack_subnets : "${subnet.name}IPv6" => module.subnet_ipv6_dual_stack.networks[index(module.subnet_ipv6_dual_stack.networks.*.name, subnet.name)].cidr_block if try(local.dual_stack_subnets[index(local.dual_stack_subnets.*.name, subnet.name)].include_ipv6, false) },
    local.default_tags
  )
}

resource "azurerm_virtual_network" "single_stack" {
  name                = "vnetss${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  location            = azurerm_resource_group.spoke.location
  address_space = [
    var.single_stack_ipv4_address_space,
  ]

  dns_servers = [var.firewall_private_ipv4]

  tags = merge(
    { for subnet in local.single_stack_subnets : "${subnet.name}IPv4" => module.subnet_ipv4_single_stack.networks[index(module.subnet_ipv4_single_stack.networks.*.name, subnet.name)].cidr_block },
    local.default_tags
  )
}

module "subnet_ipv4_single_stack" {
  source          = "hashicorp/subnets/cidr"
  base_cidr_block = var.single_stack_ipv4_address_space

  networks = [for subnet in local.single_stack_subnets : {
    name     = subnet.name
    new_bits = subnet.ipv4_bits
  }]
}

module "subnet_ipv4_dual_stack" {
  source          = "hashicorp/subnets/cidr"
  base_cidr_block = var.dual_stack_ipv4_address_space

  networks = [for subnet in local.dual_stack_subnets : {
    name     = subnet.name
    new_bits = subnet.ipv4_bits
  }]
}

module "subnet_ipv6_dual_stack" {
  source          = "hashicorp/subnets/cidr"
  base_cidr_block = var.dual_stack_ipv6_address_space

  networks = [for subnet in local.dual_stack_subnets : {
    name     = subnet.name
    new_bits = local.ipv6_bits
  }]
}

resource "azurerm_subnet" "single_stack" {
  name                 = each.key
  resource_group_name  = azurerm_resource_group.spoke.name
  virtual_network_name = azurerm_virtual_network.single_stack.name

  address_prefixes = concat(
    [module.subnet_ipv4_single_stack.networks[index(module.subnet_ipv4_single_stack.networks.*.name, each.key)].cidr_block],
  )

  dynamic "delegation" {
    content {
      name = delegation.key
      service_delegation {
        name    = delegation.value.name
        actions = delegation.value.actions
      }
    }

    for_each = try(each.value.delegations, {})
  }

  service_endpoints = try(each.value.service_endpoint, [])
  lifecycle {
    prevent_destroy = false
  }

  for_each = { for subnet in local.single_stack_subnets : subnet.name => subnet if try(subnet.create, false) }
}

resource "azurerm_subnet" "dual_stack" {
  name                 = each.key
  resource_group_name  = azurerm_resource_group.spoke.name
  virtual_network_name = azurerm_virtual_network.dual_stack.name

  address_prefixes = concat(
    [module.subnet_ipv4_dual_stack.networks[index(module.subnet_ipv4_dual_stack.networks.*.name, each.key)].cidr_block],
    each.value.include_ipv6 ? [module.subnet_ipv6_dual_stack.networks[index(module.subnet_ipv6_dual_stack.networks.*.name, each.key)].cidr_block] : [],
  )

  dynamic "delegation" {
    content {
      name = delegation.key
      service_delegation {
        name    = delegation.value.name
        actions = delegation.value.actions
      }
    }

    for_each = try(each.value.delegations, {})
  }

  service_endpoints = try(each.value.service_endpoint, [])
  lifecycle {
    prevent_destroy = false
  }

  for_each = { for subnet in local.dual_stack_subnets : subnet.name => subnet if try(subnet.create, false) }
}

resource "azurerm_route_table" "forced_tunneling" {
  name                = "rtft${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  location            = azurerm_resource_group.spoke.location
  route = [
    {
      name                   = "IPv4ForcedTunneling"
      address_prefix         = "0.0.0.0/0"
      next_hop_type          = "VirtualAppliance"
      next_hop_in_ip_address = var.firewall_private_ipv4
    },
    # {
    #   name                   = "IPv6ForcedTunneling"
    #   address_prefix         = "::/0"
    #   next_hop_type          = "VirtualAppliance"
    #   next_hop_in_ip_address = data.azurerm_app_configuration_key.firewall_private_ipv6.value
    # }
  ]
}

resource "azurerm_virtual_network_peering" "hub_to_spoke_single_stack" {
  name                         = "spoke-single-stack-${lower(var.environment)}"
  resource_group_name          = "rg${local.hub_suffix}"
  virtual_network_name         = data.azurerm_virtual_network.hub_vnet.name
  remote_virtual_network_id    = azurerm_virtual_network.single_stack.id
  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = true
  use_remote_gateways          = false

  provider = azurerm.hub
}

resource "azurerm_virtual_network_peering" "hub_to_spoke_dual_stack" {
  name                         = "spoke-dual-stack-${lower(var.environment)}"
  resource_group_name          = "rg${local.hub_suffix}"
  virtual_network_name         = data.azurerm_virtual_network.hub_vnet.name
  remote_virtual_network_id    = azurerm_virtual_network.dual_stack.id
  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = true
  use_remote_gateways          = false

  provider = azurerm.hub
}

resource "azurerm_virtual_network_peering" "spoke_to_hub_single_stack" {
  name                      = "hub"
  resource_group_name       = azurerm_resource_group.spoke.name
  virtual_network_name      = azurerm_virtual_network.single_stack.name
  remote_virtual_network_id = data.azurerm_virtual_network.hub_vnet.id

  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = false
  use_remote_gateways          = true
}

resource "azurerm_virtual_network_peering" "spoke_to_hub_dual_stack" {
  name                      = "hub"
  resource_group_name       = azurerm_resource_group.spoke.name
  virtual_network_name      = azurerm_virtual_network.dual_stack.name
  remote_virtual_network_id = data.azurerm_virtual_network.hub_vnet.id

  allow_virtual_network_access = true
  allow_forwarded_traffic      = true
  allow_gateway_transit        = false
  use_remote_gateways          = true
}
