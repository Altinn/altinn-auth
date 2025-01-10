resource "azurerm_private_dns_resolver" "resolver" {
  name                = "dnsres${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location
  virtual_network_id  = azurerm_virtual_network.hub.id

  tags = merge({}, local.default_tags)
}

resource "azurerm_private_dns_resolver_inbound_endpoint" "resolver" {
  name                    = "PrivateLink"
  private_dns_resolver_id = azurerm_private_dns_resolver.resolver.id
  location                = azurerm_resource_group.hub.location
  ip_configurations {
    private_ip_allocation_method = "Dynamic"
    subnet_id                    = azurerm_subnet.hub["DnsResolver"].id
  }
}
