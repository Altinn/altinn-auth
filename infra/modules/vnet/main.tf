locals {
  ipv4_cidr_prefix = tonumber(split("/", var.ipv4_cidr)[1])
  ipv6_cidr_prefix = tonumber(split("/", var.ipv6_cidr)[1])

  small_subnet  = 28 - local.ipv4_cidr_prefix # Available IPs 11
  medium_subnet = 25 - local.ipv4_cidr_prefix # Available IPs IPs 123
  large_subnet  = 24 - local.ipv4_cidr_prefix # Available IPs IPs 251
  xl_subnet     = 23 - local.ipv4_cidr_prefix # Available IPs 507

  ipv6_subnet = 64 - local.ipv6_cidr_prefix # must be /64 subnets

  ###! Important to not change order, resize or rename subnets once created and resource are allocated to the network.
  ###! For adding new subnets, append object only to the end of list.
  subnets = [
    {
      name         = "default"
      ipv4_bits    = local.xl_subnet
      ipv6_support = false
      nat_gateway  = true
    },
    {
      name         = "container_apps"
      ipv4_bits    = local.xl_subnet # A dedicated subnet with a CIDR range of /23 or larger is required for use with Container Apps if using the Consumption only environment.
      ipv6_support = false
      nat_gateway  = true
      service_endpoint = [
        "Microsoft.KeyVault",
        "Microsoft.Storage"
      ]
      delegations = {
        ca = {
          name = "Microsoft.App/environments"
          actions = [
            "Microsoft.Network/virtualNetworks/subnets/join/action"
          ]
        }
      }
    },
    {
      name         = "postgres"
      ipv4_bits    = local.large_subnet
      ipv6_support = false
      nat_gateway  = true
      service_endpoint = [
        "Microsoft.Storage"
      ]
      delegations = {
        fs = {
          name = "Microsoft.DBforPostgreSQL/flexibleServers"
          actions = [
            "Microsoft.Network/virtualNetworks/subnets/join/action"
          ]
        }
      }
    },
    {
      name         = "application_gateway"
      ipv4_bits    = local.medium_subnet
      ipv6_support = false
      nat_gateway  = false
    },
  ]
}

resource "azurerm_virtual_network" "vnet" {
  name = "vnet${var.metadata.suffix}"

  address_space = concat(
    [var.ipv4_cidr],
    var.use_ipv6 ? [var.ipv6_cidr] : []
  )

  resource_group_name = var.resource_group_name
  location            = var.location

  lifecycle {
    prevent_destroy = true
  }
}

module "ipv4" {
  source          = "hashicorp/subnets/cidr"
  base_cidr_block = var.ipv4_cidr

  networks = [for subnet in local.subnets : {
    name     = subnet.name
    new_bits = subnet.ipv4_bits
  }]
}

module "ipv6" {
  source          = "hashicorp/subnets/cidr"
  base_cidr_block = var.ipv6_cidr

  networks = [for subnet in local.subnets : {
    name     = subnet.name
    new_bits = local.ipv6_subnet
  }]
}

resource "azurerm_subnet" "vnet" {
  name                 = each.key
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.vnet.name

  address_prefixes = concat([module.ipv4.networks[index(module.ipv4.networks.*.name, each.key)].cidr_block, ],
    try(each.value.ipv6_support, false) ? [module.ipv6.networks[index(module.ipv6.networks.*.name, each.key)].cidr_block] : []
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

  for_each = { for subnet in local.subnets : subnet.name => subnet }
}
