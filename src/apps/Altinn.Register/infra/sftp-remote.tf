data "azurerm_key_vault_secret" "ccr_flatfile_remote_user" {
  for_each = (
    var.config.ccr.flatfiles.remote != null
    ? toset(["remote"])
    : toset([])
  )

  name         = var.config.ccr.flatfiles.remote.user
  key_vault_id = module.key_vault.id
}

data "azurerm_key_vault_secret" "ccr_flatfile_remote_pass" {
  for_each = (
    var.config.ccr.flatfiles.remote != null
    ? toset(["remote"])
    : toset([])
  )

  name         = var.config.ccr.flatfiles.remote.pass
  key_vault_id = module.key_vault.id
}

data "azurerm_key_vault_secret" "ccr_flatfile_remote_host" {
  for_each = (
    var.config.ccr.flatfiles.remote != null
    ? toset(["remote"])
    : toset([])
  )

  name         = var.config.ccr.flatfiles.remote.host
  key_vault_id = module.key_vault.id
}

data "azurerm_key_vault_secret" "ccr_flatfile_remote_path" {
  for_each = (
    var.config.ccr.flatfiles.remote != null
    ? toset(["remote"])
    : toset([])
  )

  name         = var.config.ccr.flatfiles.remote.path
  key_vault_id = module.key_vault.id
}
