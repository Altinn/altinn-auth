resource "azurerm_log_analytics_workspace" "telemetry" {
  name                = "log${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "telemetry" {
  name                = "ai${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.telemetry.id
}
