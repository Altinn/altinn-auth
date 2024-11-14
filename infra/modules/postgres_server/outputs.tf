output "id" {
  value       = azurerm_postgresql_flexible_server.postgres_server.id
  description = "Postgres Flexible server AzureRM ID"
}

output "host" {
  value = azurerm_postgresql_flexible_server.postgres_server.fqdn
}

output "name" {
  value = azurerm_postgresql_flexible_server.postgres_server.name
}
