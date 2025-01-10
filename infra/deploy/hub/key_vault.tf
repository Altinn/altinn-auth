resource "azurerm_key_vault" "key_vault" {
  name                = "kv${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location

  public_network_access_enabled = true
  purge_protection_enabled      = false
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  enable_rbac_authorization     = true
  sku_name                      = "standard"

  tags = merge({}, local.default_tags)
}

# Private DNS Zone for Key Vault
resource "azurerm_private_dns_zone" "key_vault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = azurerm_resource_group.hub.name

  tags = merge({}, local.default_tags)
}

# Link DNS Zone to Virtual Network
resource "azurerm_private_dns_zone_virtual_network_link" "key_vault" {
  name                  = "key-vault"
  resource_group_name   = azurerm_resource_group.hub.name
  private_dns_zone_name = azurerm_private_dns_zone.key_vault.name
  virtual_network_id    = azurerm_virtual_network.hub.id

  tags = merge({}, local.default_tags)
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "key_vault_administrator" {
  scope                = azurerm_key_vault.key_vault.id
  principal_id         = each.value
  role_definition_name = "Key Vault Administrator" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(var.maintainers)
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "key_vault" {
  name                          = "pepkv${local.suffix}"
  location                      = azurerm_resource_group.hub.location
  resource_group_name           = azurerm_resource_group.hub.name
  subnet_id                     = azurerm_subnet.hub["Default"].id
  custom_network_interface_name = "nickv${local.suffix}"

  private_dns_zone_group {
    name                 = "key-vault"
    private_dns_zone_ids = [azurerm_private_dns_zone.key_vault.id]
  }

  private_service_connection {
    name                           = "key-vault"
    private_connection_resource_id = azurerm_key_vault.key_vault.id
    is_manual_connection           = false
    subresource_names              = ["vault"]
  }

  tags = merge({}, local.default_tags)
}
