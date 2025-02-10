data "azurerm_private_dns_zone" "blob_storage" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = "rg${local.hub_suffix}"
  provider            = azurerm.hub
}

resource "azurerm_storage_account" "storage" {
  name                          = "st${local.suffix}"
  resource_group_name           = azurerm_resource_group.spoke.name
  location                      = azurerm_resource_group.spoke.location
  account_tier                  = "Standard"
  account_replication_type      = "GRS"
  https_traffic_only_enabled    = true
  public_network_access_enabled = true


  identity {
    type = "SystemAssigned"
  }

  tags = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_storage_container" "lease" {
  name                  = "leases"
  storage_account_id    = azurerm_storage_account.storage.id
  container_access_type = "private"
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "blob" {
  name                          = "pepstblob${local.suffix}"
  resource_group_name           = azurerm_resource_group.spoke.name
  location                      = azurerm_resource_group.spoke.location
  subnet_id                     = azurerm_subnet.dual_stack["Default"].id
  custom_network_interface_name = "nicstblob${local.suffix}"

  private_dns_zone_group {
    name                 = azurerm_storage_account.storage.name
    private_dns_zone_ids = [data.azurerm_private_dns_zone.key_vault.id]
  }

  private_service_connection {
    name                           = azurerm_storage_account.storage.name
    private_connection_resource_id = azurerm_storage_account.storage.id
    is_manual_connection           = false
    subresource_names              = ["blob"]
  }

  tags = merge({}, local.default_tags)
}
