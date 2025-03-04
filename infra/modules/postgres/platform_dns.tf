data "azurerm_private_dns_a_record" "postgres" {
  zone_name           = "privatelink.postgres.database.azure.com"
  name                = var.pg_dns_hex
  resource_group_name = "rg${var.hub_suffix}"
  provider            = azurerm.hub

  depends_on = [azurerm_postgresql_flexible_server.postgres_server]
  count      = var.pg_dns_hex == "" ? 0 : 1
}

# Poinpointed DNS server that contains just this DNS server. 
resource "azurerm_private_dns_zone" "hex" {
  name                = "${var.pg_dns_hex}.privatelink.postgres.database.azure.com"
  resource_group_name = var.resource_group_name
  count               = var.pg_dns_hex == "" ? 0 : 1
}

resource "azurerm_private_dns_a_record" "hex" {
  zone_name           = azurerm_private_dns_zone.hex[0].name
  name                = "@"
  records             = data.azurerm_private_dns_a_record.postgres[0].records
  resource_group_name = var.resource_group_name
  ttl                 = 3600
  count               = var.pg_dns_hex == "" ? 0 : 1
}
