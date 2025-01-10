resource "azurerm_app_configuration" "app_configuration" {
  name                = "appconf${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location

  local_auth_enabled    = false
  public_network_access = "Enabled"
  sku                   = "standard"

  identity {
    type = "SystemAssigned"
  }

  tags = merge({}, local.default_tags)
}

# Private DNS Zone for Key Vault
resource "azurerm_private_dns_zone" "app_configuration" {
  name                = "privatelink.azconfig.io"
  resource_group_name = azurerm_resource_group.hub.name

  tags = local.default_tags
}

# Link DNS Zone to Virtual Network
resource "azurerm_private_dns_zone_virtual_network_link" "app_configuration" {
  name                  = "app-configuration"
  resource_group_name   = azurerm_resource_group.hub.name
  private_dns_zone_name = azurerm_private_dns_zone.app_configuration.name
  virtual_network_id    = azurerm_virtual_network.hub.id
  tags                  = local.default_tags
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "app_configuration" {
  name                          = "pepappconf${local.suffix}"
  location                      = azurerm_resource_group.hub.location
  resource_group_name           = azurerm_resource_group.hub.name
  subnet_id                     = azurerm_subnet.hub["Default"].id
  custom_network_interface_name = "nicappconf${local.suffix}"

  private_dns_zone_group {
    name                 = "app-configuration"
    private_dns_zone_ids = [azurerm_private_dns_zone.app_configuration.id]
  }

  private_service_connection {
    name                           = "app-configuration"
    private_connection_resource_id = azurerm_app_configuration.app_configuration.id
    is_manual_connection           = false
    subresource_names              = ["configurationStores"]
  }

  tags = local.default_tags
}
