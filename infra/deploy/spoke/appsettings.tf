locals {
  app_settings = {
    "Platform:SblBridge:Endpoint"        = var.appconfiguration.platform_sbl_bridge_endpoint
    "Platform:ResourceRegistry:Endpoint" = var.appconfiguration.platform_resource_registry_endpoint
    "Platform:Register:Endpoint"         = var.appconfiguration.platform_register_endpoint
    "Lease:StorageAccount:BlobEndpoint"  = azurerm_storage_account.storage.primary_blob_endpoint
  }
}

module "app_configuration" {
  source     = "../../modules/appsettings"
  hub_suffix = local.hub_suffix

  key_value = [for key, value in local.app_settings :
    {
      key   = key
      value = value
      label = lower(var.environment)
  }]

  providers = {
    azurerm.hub = azurerm.hub
  }
}
