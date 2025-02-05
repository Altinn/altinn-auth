provider "azurerm" {
  alias           = "hub"
  subscription_id = var.hub_subscription_id
  features {
  }
}

data "azurerm_resource_group" "hub" {
  name     = "rg${var.hub_suffix}"
  provider = azurerm.hub
}

data "azurerm_resource_group" "spoke" {
  name = "rg${var.spoke_suffix}"
}

data "azurerm_app_configuration" "use_app_configuration" {
  name                = "appconf${var.hub_suffix}"
  resource_group_name = data.azurerm_resource_group.hub.name
  count               = var.use_app_configuration ? 1 : 0
  provider            = azurerm.hub
}

data "azurerm_servicebus_namespace" "use_masstransit" {
  name                = "sb${var.spoke_suffix}"
  resource_group_name = data.azurerm_resource_group.spoke.name
  count               = var.use_masstransit ? 1 : 0
}

data "azurerm_storage_account" "use_lease" {
  name                = "st${var.spoke_suffix}"
  resource_group_name = data.azurerm_resource_group.spoke.name
  count               = var.use_lease ? 1 : 0
}

# https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
resource "azurerm_role_assignment" "use_masstransit" {
  principal_id         = var.principal_id
  scope                = data.azurerm_servicebus_namespace.use_masstransit[0].id
  count                = var.use_masstransit ? 1 : 0
  role_definition_name = "Azure Service Bus Data Owner"
  # https://masstransit.io/documentation/configuration/transports/azure-service-bus
}

# https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
resource "azurerm_role_assignment" "use_lease" {
  principal_id         = var.principal_id
  scope                = data.azurerm_storage_account.use_lease[0].id
  count                = var.use_masstransit ? 1 : 0
  role_definition_name = "Storage Blob Data Contributor"
}

# https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
resource "azurerm_role_assignment" "use_app_configuration" {
  principal_id         = var.principal_id
  scope                = data.azurerm_app_configuration.use_app_configuration[0].id
  count                = var.use_app_configuration ? 1 : 0
  role_definition_name = "App Configuration Data Reader"
}
