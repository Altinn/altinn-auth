############################################################
# SHARED REGISTER INFRASTRUCTURE
############################################################

module "register_shared_storage_account" {
  source              = "../../modules/storage_account"
  suffix              = "${var.organization}${var.product_name}reg${var.instance}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location

  sftp_enabled = true

  queues = {
    "ccr-updates-at22"        = {}
    "ccr-updates-at22-poison" = {}

    "ccr-updates-at23"        = {}
    "ccr-updates-at23-poison" = {}
  }

  tags = merge({}, local.default_tags)

  providers = {
    azurerm = azurerm
  }
}

resource "azurerm_storage_container" "register_ccr_at22_files" {
  name                  = "ccr-at22-files"
  storage_account_id    = module.register_shared_storage_account.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "register_ccr_at23_files" {
  name                  = "ccr-at23-files"
  storage_account_id    = module.register_shared_storage_account.id
  container_access_type = "private"
}

# Terraform deploys for the environments needs to be able to maintain role-assignments on the storage account
resource "azurerm_role_assignment" "register_storage_account_cd" {
  for_each = toset(var.cd_principal_ids)

  scope                = module.register_shared_storage_account.id
  principal_id         = each.value
  role_definition_name = "User Access Administrator"
}

# Maintainers have permissions to manage the queues
resource "azurerm_role_assignment" "register_storage_account_maintainer" {
  for_each = toset(var.maintainers_principal_ids)

  scope                = module.register_shared_storage_account.id
  principal_id         = each.value
  role_definition_name = "Storage Queue Data Contributor"
}
