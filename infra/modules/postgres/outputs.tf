output "id" {
  value       = azurerm_postgresql_flexible_server.postgres_server.id
  description = "The ID of the PostgreSQL Flexible Server."
}

output "name" {
  value       = azurerm_postgresql_flexible_server.postgres_server.name
  description = "Specifies the name of the Management Lock. Changing this forces a new resource to be created."
}
