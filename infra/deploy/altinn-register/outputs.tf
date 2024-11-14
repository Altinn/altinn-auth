
output "postgres_server_name" {
  value       = module.postgres_server.name
  description = "Name of the postgres server"
}

output "key_vault_name" {
  value       = module.key_vault.name
  description = "Name of the key vault"
}

output "resource_group_name" {
  value       = azurerm_resource_group.rg.name
  description = "Resource group name"
}

output "subscription_id" {
  value       = data.azurerm_client_config.current.subscription_id
  description = "Azure subscription ID"
}
