

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
resource "azurerm_role_assignment" "storage_account_contributor" {
  scope                = azurerm_resource_group.hub.id
  principal_id         = each.value
  role_definition_name = "Storage Account Contributor" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(var.hub_principal_ids)
}

resource "azurerm_role_assignment" "storage_blob_data_owner" {
  scope                = azurerm_resource_group.hub.id
  principal_id         = each.value
  role_definition_name = "Storage Blob Data Owner" # https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#security

  for_each = toset(var.maintainers_principal_ids)
}
