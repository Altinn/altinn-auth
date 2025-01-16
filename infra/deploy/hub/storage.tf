resource "azurerm_storage_account" "storage" {
  name                          = "st${local.suffix}"
  resource_group_name           = azurerm_resource_group.hub.name
  location                      = azurerm_resource_group.hub.location
  account_tier                  = "Standard"
  account_replication_type      = "GRS"
  https_traffic_only_enabled    = true
  public_network_access_enabled = true

  identity {
    type = "SystemAssigned"
  }

  tags = merge({}, local.default_tags)
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "blob" {
  name                          = "pepstblob${local.suffix}"
  location                      = azurerm_resource_group.hub.location
  resource_group_name           = azurerm_resource_group.hub.name
  subnet_id                     = azurerm_subnet.hub["Default"].id
  custom_network_interface_name = "nicstblob${local.suffix}"

  private_dns_zone_group {
    name                 = azurerm_storage_account.storage.name
    private_dns_zone_ids = [azurerm_private_dns_zone.dns["privatelink.blob.core.windows.net"].id]
  }

  private_service_connection {
    name                           = azurerm_storage_account.storage.name
    private_connection_resource_id = azurerm_storage_account.storage.id
    is_manual_connection           = false
    subresource_names = [
      "blob"
    ]
  }

  tags = merge({}, local.default_tags)
}
