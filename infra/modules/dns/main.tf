locals {
  # https://learn.microsoft.com/en-us/azure/private-link/private-endpoint-dns#virtual-network-and-on-premises-workloads-using-a-dns-forwarder
  zones = merge(tomap({
    service_bus          = "privatelink.servicebus.windows.net"
    storage_account_blob = "privatelink.blob.core.windows.net"
    postgres             = "privatelink.postgres.database.azure.com"
    key_vault            = "privatelink.vaultcore.azure.net"
    app_configuration    = "privatelink.azconfig.io"
  }), var.domains)
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/dns_zone
resource "azurerm_private_dns_zone" "dns" {
  name                = each.value
  resource_group_name = var.resource_group_name

  tags = var.metadata

  for_each = local.zones
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/private_dns_zone_virtual_network_link
resource "azurerm_private_dns_zone_virtual_network_link" "dns" {
  name                  = each.key
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.dns[each.key].name

  virtual_network_id   = var.vnet_id
  registration_enabled = false

  tags = var.metadata
  lifecycle {
    ignore_changes = [tags]
  }

  for_each = local.zones
}
