data "azurerm_private_dns_zone" "blob_storage" {
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = "rg${local.hub_suffix}"
  provider            = azurerm.hub
}

resource "azurerm_storage_account" "storage" {
  name                     = "st${local.suffix}"
  resource_group_name      = azurerm_resource_group.spoke.name
  location                 = azurerm_resource_group.spoke.location
  account_tier             = "Premium"
  account_kind             = "BlockBlobStorage"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  blob_properties {
    versioning_enabled = true
  }

  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = true
  https_traffic_only_enabled      = true
  public_network_access_enabled   = true

  identity {
    type = "SystemAssigned"
  }

  tags = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = false
  }
}

resource "azurerm_log_analytics_workspace" "log_dwh" {
  name                = "logdwh${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  location            = azurerm_resource_group.spoke.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_log_analytics_data_export_rule" "log_dwh" {
  name                    = "StorageAccountDwh"
  resource_group_name     = azurerm_resource_group.spoke.name
  workspace_resource_id   = azurerm_log_analytics_workspace.log_dwh.id
  destination_resource_id = azurerm_storage_account.storage_dwh.id
  enabled                 = true
  table_names             = ["AzureDiagnostics", "AzureActivity"]
}

resource "azurerm_log_analytics_linked_storage_account" "log_dwh" {
  data_source_type      = each.key
  resource_group_name   = azurerm_resource_group.spoke.name
  workspace_resource_id = azurerm_log_analytics_workspace.log_dwh.id
  storage_account_ids   = [azurerm_storage_account.storage_dwh.id]
  for_each              = toset(["CustomLogs", "Query", "Alerts"])
}

resource "azurerm_storage_account" "storage_dwh" {
  name                     = "stdwh${local.suffix}"
  resource_group_name      = azurerm_resource_group.spoke.name
  location                 = azurerm_resource_group.spoke.location
  account_tier             = "Standard"
  account_replication_type = "GRS"
  min_tls_version          = "TLS1_2"

  blob_properties {
    versioning_enabled  = true
    change_feed_enabled = true
  }

  nfsv3_enabled                   = false # Must be turned off
  sftp_enabled                    = false # Must be turned off
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = true
  https_traffic_only_enabled      = true
  public_network_access_enabled   = true

  identity {
    type = "SystemAssigned"
  }

  tags = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = false
  }
}

resource "azurerm_storage_container" "lease" {
  name                  = "leases"
  storage_account_id    = azurerm_storage_account.storage.id
  container_access_type = "private"
}

resource "azurerm_private_endpoint" "blob_dwh" {
  name                          = "pepstdwhblob${local.suffix}"
  resource_group_name           = azurerm_resource_group.spoke.name
  location                      = azurerm_resource_group.spoke.location
  subnet_id                     = azurerm_subnet.dual_stack["Default"].id
  custom_network_interface_name = "nicstdwhblob${local.suffix}"

  private_dns_zone_group {
    name                 = azurerm_storage_account.storage_dwh.name
    private_dns_zone_ids = [data.azurerm_private_dns_zone.blob_storage.id]
  }

  private_service_connection {
    name                           = azurerm_storage_account.storage_dwh.name
    private_connection_resource_id = azurerm_storage_account.storage_dwh.id
    is_manual_connection           = false
    subresource_names              = ["blob"]
  }

  tags = merge({}, local.default_tags)
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
    private_dns_zone_ids = [data.azurerm_private_dns_zone.blob_storage.id]
  }

  private_service_connection {
    name                           = azurerm_storage_account.storage.name
    private_connection_resource_id = azurerm_storage_account.storage.id
    is_manual_connection           = false
    subresource_names              = ["blob"]
  }

  tags = merge({}, local.default_tags)
}
