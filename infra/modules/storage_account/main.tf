terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
  }
}

resource "azurerm_storage_account" "storage" {
  name                     = "st${var.prefix}${var.suffix}"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "GRS"

  allow_nested_items_to_be_public = false
  public_network_access_enabled   = true

  # Must be enabled otherwise terraform can't create sub-resources like queues/blobs/files
  shared_access_key_enabled = true
  identity {
    type = "SystemAssigned"
  }

  tags = var.tags

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_storage_queue" "queues" {
  for_each = var.queues

  name               = each.key
  storage_account_id = azurerm_storage_account.storage.id
}
