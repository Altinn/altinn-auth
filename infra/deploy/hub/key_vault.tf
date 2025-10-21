resource "azurerm_key_vault" "key_vault" {
  name                = "kv${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location

  public_network_access_enabled = true
  purge_protection_enabled      = false
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  rbac_authorization_enabled    = true
  sku_name                      = "standard"

  tags = merge({}, local.default_tags)

  lifecycle {
    prevent_destroy = true
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "key_vault_administrator" {
  scope                = azurerm_key_vault.key_vault.id
  principal_id         = each.value
  role_definition_name = "Key Vault Administrator" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(var.maintainers_principal_ids)
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "key_vault" {
  name                          = "pepkv${local.suffix}"
  location                      = azurerm_resource_group.hub.location
  resource_group_name           = azurerm_resource_group.hub.name
  subnet_id                     = azurerm_subnet.hub["Default"].id
  custom_network_interface_name = "nickv${local.suffix}"

  private_dns_zone_group {
    name                 = "privatelink.vaultcore.azure.net"
    private_dns_zone_ids = [azurerm_private_dns_zone.dns["privatelink.vaultcore.azure.net"].id]
  }

  private_service_connection {
    name                           = azurerm_key_vault.key_vault.name
    private_connection_resource_id = azurerm_key_vault.key_vault.id
    is_manual_connection           = false
    subresource_names              = ["vault"]
  }

  tags = merge({}, local.default_tags)
}
