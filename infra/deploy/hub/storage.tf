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

# Private DNS Zone for Key Vault
resource "azurerm_private_dns_zone" "blob" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = azurerm_resource_group.hub.name

  tags = local.default_tags
}

# Link DNS Zone to Virtual Network
resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  name                  = "storage-account-blob"
  resource_group_name   = azurerm_resource_group.hub.name
  private_dns_zone_name = azurerm_private_dns_zone.blob.name
  virtual_network_id    = azurerm_virtual_network.hub.id
  tags                  = local.default_tags
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "blob" {
  name                          = "pepstblob${local.suffix}"
  location                      = azurerm_resource_group.hub.location
  resource_group_name           = azurerm_resource_group.hub.name
  subnet_id                     = azurerm_subnet.hub["Default"].id
  custom_network_interface_name = "nicstblob${local.suffix}"

  private_dns_zone_group {
    name                 = "storage-account-blob"
    private_dns_zone_ids = [azurerm_private_dns_zone.blob.id]
  }

  private_service_connection {
    name                           = "storage-account-blob"
    private_connection_resource_id = azurerm_storage_account.storage.id
    is_manual_connection           = false
    subresource_names = [
      "blob"
    ]
  }

  tags = merge({}, local.default_tags)
}
