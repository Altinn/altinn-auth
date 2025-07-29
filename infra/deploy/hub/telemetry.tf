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

resource "azurerm_dashboard_grafana" "grafana" {
  name                  = "grafana${local.suffix}"
  location              = azurerm_resource_group.hub.location
  resource_group_name   = azurerm_resource_group.hub.name
  sku                   = "Standard"
  grafana_major_version = 11
  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_role_assignment" "grafana_admin" {
  scope                = azurerm_resource_group.hub.id
  role_definition_name = "Grafana Admin"
  principal_id         = each.value
  for_each             = toset(var.maintainers_principal_ids)
}

resource "azurerm_role_assignment" "grafana_editors" {
  scope                = azurerm_resource_group.hub.id
  role_definition_name = "Grafana Editor"
  principal_id         = each.value
  for_each             = toset(var.developer_prod_principal_ids)
}
