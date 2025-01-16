
locals {
  zones = [
    "privatelink.servicebus.windows.net",
    "privatelink.blob.core.windows.net",
    "privatelink.postgres.database.azure.com",
    "privatelink.vaultcore.azure.net",
    "privatelink.azconfig.io"
  ]
}

resource "azurerm_private_dns_zone" "dns" {
  name                = each.value
  resource_group_name = azurerm_resource_group.hub.name

  tags     = merge({}, local.default_tags)
  for_each = toset(local.zones)
}

resource "azurerm_private_dns_zone_virtual_network_link" "dns" {
  name                  = each.key
  resource_group_name   = azurerm_resource_group.hub.name
  private_dns_zone_name = azurerm_private_dns_zone.dns[each.key].name

  virtual_network_id   = azurerm_virtual_network.hub.id
  registration_enabled = false

  tags     = merge({}, local.default_tags)
  for_each = toset(local.zones)
}
