terraform {
  required_providers {
    pkcs12 = {
      source  = "chilicat/pkcs12"
      version = "0.2.5"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "4.0.6"
    }
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.23.0"
    }
    static = {
      source  = "tiwood/static"
      version = "0.1.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "3.1.0"
    }
  }

  backend "azurerm" {
    use_azuread_auth = true
  }
}

provider "azurerm" {
  # The Files Storage API does not support authenticating via AzureAD
  # and will continue to use a SharedKey when AAD authentication is enabled.
  # Thus storage_use_azuread must be disabled until azure supports it  
  # storage_use_azuread = true 
  features {
  }
}

data "azurerm_client_config" "current" {}

locals {
  suffix = "${var.organization}${var.product_name}${var.instance}hub"

  ipv4_cidr_prefix = tonumber(split("/", var.single_stack_ipv4_address_space)[1])
  ipv6_cidr_prefix = tonumber(split("/", var.dual_stack_ipv6_address_space)[1])
  ipv6_bits        = 64 - local.ipv6_cidr_prefix

  default_tags = {
    ProductName = var.product_name
    Environment = "hub"
    Instance    = "001"
    CreatedAt   = try(static_data.static.output.created_at, timestamp())
  }

  # Do not change order of list, nor name or address space. Append only!
  subnets = [
    {
      name         = "Default"
      include_ipv6 = true
      ipv4_bits    = 24 - local.ipv4_cidr_prefix
      service_endpoint = [
        "Microsoft.KeyVault",
      ]
    },
    {
      name         = "GatewaySubnet"
      include_ipv6 = true
      ipv4_bits    = 24 - local.ipv4_cidr_prefix
    },
    {
      name         = "ApplicationGateway"
      include_ipv6 = true
      ipv4_bits    = 24 - local.ipv4_cidr_prefix
    },
    {
      name         = "AzureFirewallSubnet"
      include_ipv6 = true
      ipv4_bits    = 26 - local.ipv4_cidr_prefix
    },
    {
      name         = "AzureFirewallManagementSubnet"
      include_ipv6 = true
      ipv4_bits    = 26 - local.ipv4_cidr_prefix
    },
    {
      name         = "DnsResolver"
      include_ipv6 = false # Does not support IPv6
      ipv4_bits    = 24 - local.ipv4_cidr_prefix
      delegations = {
        dns_resolver = {
          name = "Microsoft.Network/dnsResolvers"
          actions = [
            "Microsoft.Network/virtualNetworks/subnets/join/action"
          ]
        }
      }
    }
  ]
}

resource "static_data" "static" {
  data = {
    api_id     = uuid()
    created_at = formatdate("EEEE, DD-MMM-YY hh:mm:ss ZZZ", "2018-01-02T23:12:01Z")
  }

  lifecycle {
    ignore_changes = [data]
  }
}

resource "azurerm_resource_group" "hub" {
  name     = "rg${local.suffix}"
  location = "norwayeast"

  tags = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_virtual_network" "hub" {
  name                = "vnet${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location
  address_space = [
    var.dual_stack_ipv4_address_space,
    var.dual_stack_ipv6_address_space
  ]

  tags = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_virtual_network_dns_servers" "hub" {
  virtual_network_id = azurerm_virtual_network.hub.id
  dns_servers        = [azurerm_private_dns_resolver_inbound_endpoint.resolver.ip_configurations[0].private_ip_address]
}

module "subnet_ipv4" {
  source          = "hashicorp/subnets/cidr"
  base_cidr_block = var.dual_stack_ipv4_address_space

  networks = [for subnet in local.subnets : {
    name     = subnet.name
    new_bits = subnet.ipv4_bits
  }]
}

module "subnet_ipv6" {
  source          = "hashicorp/subnets/cidr"
  base_cidr_block = var.dual_stack_ipv6_address_space

  networks = [for subnet in local.subnets : {
    name     = subnet.name
    new_bits = local.ipv6_bits
  }]
}

resource "azurerm_subnet" "hub" {
  name                 = each.key
  resource_group_name  = azurerm_resource_group.hub.name
  virtual_network_name = azurerm_virtual_network.hub.name

  address_prefixes = concat(
    [module.subnet_ipv4.networks[index(module.subnet_ipv4.networks.*.name, each.key)].cidr_block],
    each.value.include_ipv6 ? [module.subnet_ipv6.networks[index(module.subnet_ipv6.networks.*.name, each.key)].cidr_block] : [],
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

resource "azurerm_public_ip_prefix" "ipv4" {
  name                = "pipipv4${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location
  sku                 = "Standard"
  ip_version          = "IPv4"

  prefix_length = 30 # 4 Public IPs
  tags          = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_public_ip_prefix" "ipv6" {
  name                = "pipipv6${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location
  sku                 = "Standard"
  ip_version          = "IPv6"

  prefix_length = 126 # 4 Public IPs
  tags          = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_role_assignment" "network_contributor" {
  scope                = azurerm_resource_group.hub.id
  principal_id         = each.value
  role_definition_name = "Network Contributor" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(var.spoke_principal_ids)
}

resource "azurerm_role_assignment" "reader" {
  scope                = azurerm_resource_group.hub.id
  principal_id         = each.value
  role_definition_name = "Reader" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(var.spoke_principal_ids)
}

resource "azurerm_management_lock" "delete" {
  name       = "Terraform"
  scope      = each.value
  lock_level = "CanNotDelete"
  notes      = "Terraform Managed Lock"

  for_each = { for lock in [
    azurerm_public_ip_prefix.ipv4,
    azurerm_public_ip_prefix.ipv6,
    azurerm_virtual_network_gateway.vpn,
    azurerm_storage_account.storage
  ] : lock.name => lock.id }
}
