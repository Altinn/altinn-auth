# NOTES
# * You only see the Networking tab for premium namespaces. To set IP firewall rules for the other tiers, use Azure Resource Manager templates, Azure CLI, PowerShell or REST API.
# * Private endpoint and CMK encryption are only available in Premium SKU
# * Capacity and Partitions are only allowed to set if SKU is premium
# * Using User Assigned Identity due to problematic execution order for System assigned identity and enabling CMK encryption

locals {
  service_bus_enable_public_endpoint = !var.is_prod_like
  service_bus_sku                    = var.is_prod_like ? "Premium" : "Standard"
  service_bus_enable_local_auth      = !var.is_prod_like

  service_bus_enable_private_endpoint      = var.is_prod_like         # Only avaiable for Premium tier
  service_bus_enable_encryption_at_rest    = var.is_prod_like         # Only avaiable for Premium tier
  service_bus_capacity                     = var.is_prod_like ? 1 : 0 # Only avaiable for Premium tier
  service_bus_premium_messaging_partitions = var.is_prod_like ? 1 : 0 # Only avaiable for Premium tier
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/servicebus_namespace
resource "azurerm_servicebus_namespace" "service_bus" {
  name                         = "sbaltinn${var.metadata.suffix}"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  sku                          = local.service_bus_sku
  local_auth_enabled           = local.service_bus_enable_local_auth
  capacity                     = local.service_bus_capacity
  premium_messaging_partitions = local.service_bus_premium_messaging_partitions

  network_rule_set {
    default_action                = "Deny"
    public_network_access_enabled = local.service_bus_enable_public_endpoint
    ip_rules                      = var.permitted_ip_addresses
    trusted_services_allowed      = true
  }

  identity {
    type = "SystemAssigned"
  }

  tags = var.metadata
  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags]
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/private_endpoint
resource "azurerm_private_endpoint" "service_bus_private_endpoint" {
  name                          = "pe${azurerm_servicebus_namespace.service_bus.name}"
  resource_group_name           = var.resource_group_name
  location                      = var.location
  subnet_id                     = var.subnet_id
  custom_network_interface_name = "nic${azurerm_servicebus_namespace.service_bus.name}"

  count = local.service_bus_enable_private_endpoint ? 1 : 0

  private_service_connection {
    name                           = azurerm_servicebus_namespace.service_bus.name
    private_connection_resource_id = azurerm_servicebus_namespace.service_bus.id
    is_manual_connection           = false
    subresource_names              = ["namespace"]
  }

  private_dns_zone_group {
    name                 = azurerm_servicebus_namespace.service_bus.name
    private_dns_zone_ids = var.dns_zones
  }

  tags = var.metadata
  lifecycle {
    prevent_destroy = true
  }
}

# Service bus Actions List: https://learn.microsoft.com/en-us/azure/role-based-access-control/permissions/integration#microsoftservicebus
# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_definition
resource "azurerm_role_definition" "service_bus_masstransit" {
  name        = "Azure Service Bus Mass Transit ${upper(var.metadata.environment)}"
  scope       = azurerm_servicebus_namespace.service_bus.id
  description = "Allow C# Applications use MassTransit with Azure Service Bus"

  permissions {
    actions = [
      "Microsoft.ServiceBus/namespaces/read",
      "Microsoft.ServiceBus/namespaces/queues/*",
      "Microsoft.ServiceBus/namespaces/topics/*"
    ]
  }

  lifecycle {
    prevent_destroy = true
  }

  assignable_scopes = [azurerm_servicebus_namespace.service_bus.id]
}
