data "azurerm_client_config" "current" {}

locals {
  admins = merge(
    { "current" : data.azurerm_client_config.current.object_id },
    var.entraid_admins
  )
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/key_vault
resource "azurerm_key_vault" "key_vault" {
  name                = "kvaltinn${var.metadata.suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  tenant_id           = var.tenant_id

  sku_name                  = "standard"
  enable_rbac_authorization = true
  purge_protection_enabled  = true

  soft_delete_retention_days    = 30
  public_network_access_enabled = false

  network_acls {
    bypass         = "AzureServices"
    default_action = "Allow"
  }

  tags = var.metadata
  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags]
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "key_vault_administrator" {
  scope                = azurerm_key_vault.key_vault.id
  principal_id         = each.value
  role_definition_name = "Key Vault Administrator" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security
  for_each             = local.admins
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/private_endpoint
resource "azurerm_private_endpoint" "key_vault" {
  name                          = "pe${azurerm_key_vault.key_vault.name}"
  location                      = var.location
  resource_group_name           = var.resource_group_name
  subnet_id                     = var.subnet_id
  custom_network_interface_name = "nic${azurerm_key_vault.key_vault.name}"

  private_service_connection {
    name                           = azurerm_key_vault.key_vault.name
    private_connection_resource_id = azurerm_key_vault.key_vault.id
    is_manual_connection           = false
    subresource_names              = ["vault"]
  }

  private_dns_zone_group {
    name                 = azurerm_key_vault.key_vault.name
    private_dns_zone_ids = var.dns_zones
  }

  tags = var.metadata
}

