locals {
  postgres_server_sku = var.is_prod_like ? "GP_Standard_D4s_v3" : "B_Standard_B1ms"
}


# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/postgresql_flexible_server
resource "azurerm_postgresql_flexible_server" "postgres_server" {
  name                = "psqlsrvaltinn${var.metadata.suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  version             = var.postgres_version

  delegated_subnet_id           = var.subnet_id
  private_dns_zone_id           = var.dns_zone
  public_network_access_enabled = false

  storage_mb        = var.storage_mb
  auto_grow_enabled = true

  authentication {
    active_directory_auth_enabled = true
    password_auth_enabled         = false
    tenant_id                     = var.tenant_id
  }

  sku_name = local.postgres_server_sku

  tags = var.metadata
  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags, zone]
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/postgresql_flexible_server_active_directory_administrator
resource "azurerm_postgresql_flexible_server_active_directory_administrator" "admin" {
  server_name         = azurerm_postgresql_flexible_server.postgres_server.name
  resource_group_name = var.resource_group_name
  tenant_id           = var.tenant_id

  object_id      = each.value.principal_id
  principal_name = each.value.principal_name
  principal_type = each.value.principal_type

  for_each = { for value in var.entraid_admins : value.principal_name => value }
}
