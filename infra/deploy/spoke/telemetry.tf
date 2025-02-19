
resource "azurerm_log_analytics_workspace" "telemetry" {
  name                = "log${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  location            = azurerm_resource_group.spoke.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "telemetry" {
  name                = "ai${local.suffix}"
  resource_group_name = azurerm_resource_group.spoke.name
  location            = azurerm_resource_group.spoke.location
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.telemetry.id
}

resource "azurerm_log_analytics_linked_storage_account" "telemetry" {
  data_source_type      = each.key
  resource_group_name   = azurerm_resource_group.spoke.name
  workspace_resource_id = azurerm_log_analytics_workspace.log_dwh.id
  storage_account_ids   = [azurerm_storage_account.storage_dwh.id]
  for_each              = toset(["CustomLogs", "Query", "Alerts"])
}
