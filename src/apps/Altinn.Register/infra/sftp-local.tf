
data "azurerm_storage_container" "ccr_flatfiles" {
  for_each = (
    var.config.ccr.flatfiles.enable && var.config.ccr.flatfiles.local != null
    ? toset(["local"])
    : toset([])
  )

  name = var.config.ccr.flatfiles.local.container
  storage_account_id = data.azurerm_storage_account.ccr_federate_storage_account[
    local.reg_federate_storage_account_name
  ].id

  provider = azurerm.hub
}

resource "azurerm_storage_account_local_user" "ccr_sftp_local_user" {
  for_each = (
    var.config.ccr.flatfiles.enable && var.config.ccr.flatfiles.local != null
    ? toset(["local"])
    : toset([])
  )

  name = var.config.ccr.flatfiles.local.user
  storage_account_id = data.azurerm_storage_account.ccr_federate_storage_account[
    local.reg_federate_storage_account_name
  ].id

  # azure generates the password
  ssh_password_enabled = true

  permission_scope {
    permissions {
      read   = true
      write  = true
      delete = true
      create = true
      list   = true
    }

    service       = "blob"
    resource_name = data.azurerm_storage_container.ccr_flatfiles["local"].name
  }

  home_directory = data.azurerm_storage_container.ccr_flatfiles["local"].name

  provider = azurerm.hub
}

resource "azurerm_key_vault_secret" "ccr_flatfile_local_user" {
  for_each = (
    var.config.ccr.flatfiles.enable && var.config.ccr.flatfiles.local != null
    ? toset(["local"])
    : toset([])
  )

  name = "ccr-flatfile-local-user"
  value_wo = "${data.azurerm_storage_account.ccr_federate_storage_account[
    local.reg_federate_storage_account_name
  ].name}.${azurerm_storage_account_local_user.ccr_sftp_local_user["local"].name}"
  key_vault_id = module.key_vault.id

  value_wo_version = 1
}

resource "azurerm_key_vault_secret" "ccr_flatfile_local_pass" {
  for_each = (
    var.config.ccr.flatfiles.enable && var.config.ccr.flatfiles.local != null
    ? toset(["local"])
    : toset([])
  )

  name         = "ccr-flatfile-local-pass"
  value_wo     = azurerm_storage_account_local_user.ccr_sftp_local_user["local"].password
  key_vault_id = module.key_vault.id

  value_wo_version = 1
}

resource "azurerm_key_vault_secret" "ccr_flatfile_local_host" {
  for_each = (
    var.config.ccr.flatfiles.enable && var.config.ccr.flatfiles.local != null
    ? toset(["local"])
    : toset([])
  )

  name = "ccr-flatfile-local-host"
  value_wo = data.azurerm_storage_account.ccr_federate_storage_account[
    local.reg_federate_storage_account_name
  ].primary_blob_host
  key_vault_id = module.key_vault.id

  value_wo_version = 1
}

resource "azurerm_key_vault_secret" "ccr_flatfile_local_path" {
  for_each = (
    var.config.ccr.flatfiles.enable && var.config.ccr.flatfiles.local != null
    ? toset(["local"])
    : toset([])
  )

  name         = "ccr-flatfile-local-path"
  value_wo     = "ccr"
  key_vault_id = module.key_vault.id

  value_wo_version = 1
}
