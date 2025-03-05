terraform {
  required_providers {
    azurerm = {
      source                = "hashicorp/azurerm"
      configuration_aliases = [azurerm.hub]
    }
  }
}

data "azurerm_client_config" "current" {}

locals {
  sku = {
    "Burstable"       = "B",
    "GeneralPurpose"  = "GP",
    "MemoryOptimized" = "MO"
  }
  sku_name = "${local.sku[var.compute_tier]}_${var.compute_size}"
}

resource "random_password" "pass" {
  length           = 30
  special          = true
  override_special = "@#%*()-_=+[]{}:?"
  keepers = {
    trigger = timestamp()
  }
}

data "azurerm_virtual_network" "hub" {
  name                = "vnet${var.hub_suffix}"
  resource_group_name = "rg${var.hub_suffix}"
  provider            = azurerm.hub
}

# Pinpointed DNS server that contains just this pgsqlsrv. 
resource "azurerm_private_dns_zone" "postgres" {
  name                = "psqlsrv${var.prefix}${var.suffix}.auth.postgres.database.azure.com"
  resource_group_name = var.resource_group_name
}

resource "azurerm_private_dns_zone_virtual_network_link" "link" {
  name                = data.azurerm_virtual_network.hub.name
  resource_group_name = var.resource_group_name

  virtual_network_id    = data.azurerm_virtual_network.hub.id
  private_dns_zone_name = azurerm_private_dns_zone.postgres.name
}

resource "azurerm_postgresql_flexible_server" "postgres_server" {
  name                = "psqlsrv${var.prefix}${var.suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  version             = var.postgres_version

  delegated_subnet_id           = var.subnet_id
  private_dns_zone_id           = azurerm_private_dns_zone.postgres.id
  public_network_access_enabled = false


  storage_mb        = var.storage_mb
  auto_grow_enabled = true
  storage_tier      = var.tier

  administrator_login          = "NotInUse"
  administrator_password       = random_password.pass.result
  backup_retention_days        = var.backup_retention_days
  geo_redundant_backup_enabled = true

  authentication {
    active_directory_auth_enabled = true
    password_auth_enabled         = true
    tenant_id                     = data.azurerm_client_config.current.tenant_id
  }

  create_mode = "Default"
  sku_name    = local.sku_name

  lifecycle {
    ignore_changes  = [zone]
    prevent_destroy = true
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

resource "azurerm_postgresql_flexible_server_configuration" "configuration" {
  server_id = azurerm_postgresql_flexible_server.postgres_server.id
  name      = each.key
  value     = each.value
  for_each  = var.configurations
}

resource "azurerm_management_lock" "postgres" {
  name       = "Terraform Managed Lock"
  scope      = azurerm_postgresql_flexible_server.postgres_server.id
  lock_level = "CanNotDelete"
  notes      = "Prevents unauthorized user activity to delete postgres server"
}

# sleep for 20 seconds in order for admin change(s) to propegates.
# Bootstrap may fail if ran too fast.
resource "time_sleep" "wait_30_seconds" {
  depends_on      = [azurerm_postgresql_flexible_server_configuration.configuration]
  create_duration = "30s"
}
