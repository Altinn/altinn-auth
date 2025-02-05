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

resource "azurerm_role_assignment" "app_configuration_data_owner" {
  scope                = azurerm_app_configuration.app_configuration.id
  principal_id         = each.value
  role_definition_name = "App Configuration Data Owner" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(concat(var.maintainers_principal_ids, var.spoke_principal_ids))
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "user_access_administrator" {
  scope                = azurerm_app_configuration.app_configuration.id
  principal_id         = each.value
  role_definition_name = "User Access Administrator" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(concat(var.maintainers_principal_ids, var.spoke_principal_ids))
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "app_configuration_contributor" {
  scope                = azurerm_app_configuration.app_configuration.id
  principal_id         = each.value
  role_definition_name = "App Configuration Contributor" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(concat(var.developer_prod_principal_ids))
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "app_configuration_data_reader" {
  scope                = azurerm_app_configuration.app_configuration.id
  principal_id         = each.value
  role_definition_name = "App Configuration Data Reader" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(concat(var.developer_dev_principal_ids, var.spoke_principal_ids))
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "app_configuration" {
  name                          = "pepappconf${local.suffix}"
  location                      = azurerm_resource_group.hub.location
  resource_group_name           = azurerm_resource_group.hub.name
  subnet_id                     = azurerm_subnet.hub["Default"].id
  custom_network_interface_name = "nicappconf${local.suffix}"

  private_dns_zone_group {
    name                 = azurerm_app_configuration.app_configuration.name
    private_dns_zone_ids = [azurerm_private_dns_zone.dns["privatelink.azconfig.io"].id]
  }

  private_service_connection {
    name                           = azurerm_app_configuration.app_configuration.name
    private_connection_resource_id = azurerm_app_configuration.app_configuration.id
    is_manual_connection           = false
    subresource_names              = ["configurationStores"]
  }

  tags = local.default_tags
}
