data "azurerm_client_config" "current" {}

provider "azurerm" {
  alias           = "hub"
  subscription_id = var.hub_subscription_id
  features {
  }
}

data "azurerm_resource_group" "key_vault" {
  name = var.resource_group_name
}

data "azurerm_private_dns_zone" "key_vault" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = "rg${var.hub_suffix}"
  provider            = azurerm.hub
}

data "azurerm_log_analytics_workspace" "log_dwh" {
  name                = "logdwh${var.suffix}"
  resource_group_name = "rg${var.suffix}"
}

resource "azurerm_role_assignment" "rbac" {
  role_definition_name = each.value.role_definition_name
  scope                = azurerm_key_vault.key_vault.id
  principal_id         = each.value.principal_id
  for_each             = { for value in var.key_vault_roles : value.operation_id => value }
}

resource "azurerm_key_vault" "key_vault" {
  name                          = "kv${var.name}${var.suffix}"
  resource_group_name           = data.azurerm_resource_group.key_vault.name
  location                      = data.azurerm_resource_group.key_vault.location
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  public_network_access_enabled = true
  purge_protection_enabled      = false
  rbac_authorization_enabled    = true
  sku_name                      = "standard"
  lifecycle {
    prevent_destroy = true
  }
}

# Private Endpoint for Key Vault
resource "azurerm_private_endpoint" "key_vault" {
  name                          = "pep${azurerm_key_vault.key_vault.name}"
  resource_group_name           = data.azurerm_resource_group.key_vault.name
  location                      = data.azurerm_resource_group.key_vault.location
  subnet_id                     = var.subnet_id
  custom_network_interface_name = "nickv${azurerm_key_vault.key_vault.name}"
  private_dns_zone_group {
    name                 = "privatelink.vaultcore.azure.net"
    private_dns_zone_ids = [data.azurerm_private_dns_zone.key_vault.id]
  }
  private_service_connection {
    name                           = azurerm_key_vault.key_vault.name
    private_connection_resource_id = azurerm_key_vault.key_vault.id
    is_manual_connection           = false
    subresource_names              = ["vault"]
  }
}

resource "azurerm_monitor_diagnostic_setting" "key_vault_diagnostics" {
  target_resource_id         = azurerm_key_vault.key_vault.id
  name                       = data.azurerm_log_analytics_workspace.log_dwh.name
  log_analytics_workspace_id = data.azurerm_log_analytics_workspace.log_dwh.id

  enabled_log {
    category_group = "allLogs"
  }

  enabled_log {
    category_group = "audit"
  }

  enabled_metric {
    category = "AllMetrics"
  }
}
