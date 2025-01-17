data "azurerm_client_config" "current" {}

locals {
  sku = {
    "Burstable"       = "B",
    "GeneralPurpose"  = "GP",
    "MemoryOptimized" = "MO"
  }
  sku_name = "${local.sku[var.compute_tier]}_${var.compute_size}"
}

resource "azurerm_postgresql_flexible_server" "postgres_server" {
  name                = "psqlsrv${var.name}${var.suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  version             = var.postgres_version

  delegated_subnet_id           = var.subnet_id
  private_dns_zone_id           = var.private_dns_zone_id
  public_network_access_enabled = false

  storage_mb        = var.storage_mb
  auto_grow_enabled = true
  storage_tier      = var.tier

  administrator_login    = null
  administrator_password = null
  backup_retention_days  = var.backup_retention_days

  authentication {
    active_directory_auth_enabled = true
    password_auth_enabled         = false
    tenant_id                     = data.azurerm_client_config.current.tenant_id
  }

  create_mode = "Default"
  sku_name    = local.sku_name

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags, zone]
  }

  tags = var.tags
}

resource "azurerm_postgresql_flexible_server_active_directory_administrator" "admin" {
  server_name         = azurerm_postgresql_flexible_server.postgres_server.name
  resource_group_name = var.resource_group_name
  tenant_id           = data.azurerm_client_config.current.tenant_id

  object_id      = each.value.principal_id
  principal_name = each.value.principal_name
  principal_type = each.value.principal_type

  for_each = { for value in var.entraid_admins : value.principal_id => value }
}
