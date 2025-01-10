
resource "azurerm_public_ip" "frontend_ipv4" {
  name                = "pipfwipv4${local.suffix}"
  location            = azurerm_resource_group.hub.location
  resource_group_name = azurerm_resource_group.hub.name
  allocation_method   = "Static"
  ip_version          = "IPv4"
  public_ip_prefix_id = azurerm_public_ip_prefix.ipv4.id

  tags = merge({}, local.default_tags)
}

resource "azurerm_public_ip" "management_ipv4" {
  name                = "pipfwmgmtipv4${local.suffix}"
  location            = azurerm_resource_group.hub.location
  resource_group_name = azurerm_resource_group.hub.name
  allocation_method   = "Static"
  ip_version          = "IPv4"
  public_ip_prefix_id = azurerm_public_ip_prefix.ipv4.id

  tags = merge({}, local.default_tags)
}

resource "azurerm_firewall" "firewall" {
  name                = "fw${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location

  sku_name           = "AZFW_VNet"
  sku_tier           = "Standard"
  firewall_policy_id = azurerm_firewall_policy.firewall.id

  ip_configuration {
    name                 = "FrontendIPv4PublicIP"
    public_ip_address_id = azurerm_public_ip.frontend_ipv4.id
    subnet_id            = azurerm_subnet.hub["AzureFirewallSubnet"].id
  }

  management_ip_configuration {
    name                 = "ManagementIPv4PublicIP"
    subnet_id            = azurerm_subnet.hub["AzureFirewallManagementSubnet"].id
    public_ip_address_id = azurerm_public_ip.management_ipv4.id
  }

  tags = merge({}, local.default_tags)
}

resource "azurerm_firewall_policy" "firewall" {
  name                = "fwpolicy${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location

  tags = merge({}, local.default_tags)
}
