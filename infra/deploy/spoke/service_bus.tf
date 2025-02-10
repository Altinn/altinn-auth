# NOTES
# * You only see the Networking tab for premium namespaces. To set IP firewall rules for the other tiers, use Azure Resource Manager templates, Azure CLI, PowerShell or REST API.
# * Private endpoint and CMK encryption are only available in Premium SKU
# * Capacity and Partitions are only allowed to set if SKU is premium
# * Using User Assigned Identity due to problematic execution order for System assigned identity and enabling CMK encryption

locals {
  service_bus_enable_public_endpoint = !var.prod_like
  service_bus_sku                    = var.prod_like ? "Premium" : "Standard"
  service_bus_enable_local_auth      = !var.prod_like

  service_bus_enable_private_endpoint      = var.prod_like         # Only avaiable for Premium tier
  service_bus_enable_encryption_at_rest    = var.prod_like         # Only avaiable for Premium tier
  service_bus_capacity                     = var.prod_like ? 1 : 0 # Only avaiable for Premium tier
  service_bus_premium_messaging_partitions = var.prod_like ? 1 : 0 # Only avaiable for Premium tier
}


data "azurerm_private_dns_zone" "hub_service_bus" {
  name                = "privatelink.servicebus.windows.net"
  resource_group_name = "rg${local.hub_suffix}"
  provider            = azurerm.hub
}

resource "azurerm_servicebus_namespace" "service_bus" {
  name                = "sb${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  location            = azurerm_resource_group.spoke.location

  sku                          = local.service_bus_sku
  local_auth_enabled           = local.service_bus_enable_local_auth
  capacity                     = local.service_bus_capacity
  premium_messaging_partitions = local.service_bus_premium_messaging_partitions

  network_rule_set {
    default_action                = "Deny"
    public_network_access_enabled = local.service_bus_enable_public_endpoint
    ip_rules                      = [var.firewall_public_ipv4]
    trusted_services_allowed      = true
  }

  identity {
    type = "SystemAssigned"
  }

  tags = merge({}, local.default_tags)
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/private_endpoint
resource "azurerm_private_endpoint" "service_bus_private_endpoint" {
  name                          = "pep${azurerm_servicebus_namespace.service_bus.name}"
  resource_group_name           = azurerm_resource_group.spoke.name
  location                      = azurerm_resource_group.spoke.location
  subnet_id                     = azurerm_subnet.dual_stack["ServiceBus"].id
  custom_network_interface_name = "nicsb${azurerm_servicebus_namespace.service_bus.name}"

  count = local.service_bus_enable_private_endpoint ? 1 : 0

  private_service_connection {
    name                           = azurerm_servicebus_namespace.service_bus.name
    private_connection_resource_id = azurerm_servicebus_namespace.service_bus.id
    is_manual_connection           = false
    subresource_names              = ["namespace"]
  }

  private_dns_zone_group {
    name                 = azurerm_servicebus_namespace.service_bus.name
    private_dns_zone_ids = [data.azurerm_private_dns_zone.hub_service_bus.id]
  }
}
