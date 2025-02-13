terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.18.0"
    }
  }

  backend "azurerm" {
    use_azuread_auth = true
  }
}


locals {
  repository          = "github.com/altinn/altinn-authorization"
  environment         = lower(var.environment)
  name                = "auth"
  resource_group_name = "rg${local.metadata.suffix}"

  domains = {
    api      = var.api_domain
    frontend = var.frontend_domain
  }

  metadata = {
    name        = local.name
    environment = local.environment
    instance    = var.instance
    suffix      = "${local.name}${var.instance}${var.environment}"
    repository  = local.repository
  }
}

data "azurerm_client_config" "current" {}

data "azurerm_subscription" "subscription" {
  subscription_id = data.azurerm_client_config.current.subscription_id
}

resource "azurerm_resource_group" "auth" {
  name     = local.resource_group_name
  location = var.location

  tags = local.metadata
  lifecycle {
    prevent_destroy = true
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/data-sources/user_assigned_identity
resource "azurerm_user_assigned_identity" "application_admin" {
  name                = "miappadmin${local.metadata.suffix}"
  resource_group_name = local.resource_group_name
  location            = var.location

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags]
  }
}

# https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/role_assignment
# https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#general
resource "azurerm_role_assignment" "reader" {
  scope                = data.azurerm_subscription.subscription.id
  principal_id         = azurerm_user_assigned_identity.application_admin.principal_id
  role_definition_name = "Reader"
}

module "vnet" {
  source              = "../../modules/vnet"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location

  ipv4_cidr = var.ipv4_cidr
  ipv6_cidr = var.ipv6_cidr

  depends_on = [azurerm_resource_group.auth]
}

module "nat" {
  source              = "../../modules/nat_gateway"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location

  subnets = module.vnet.subnets

  depends_on = [azurerm_resource_group.auth]
}

module "dns" {
  source              = "../../modules/dns"
  metadata            = local.metadata
  resource_group_name = azurerm_resource_group.auth.name

  vnet_id = module.vnet.id
  domains = local.domains

  depends_on = [azurerm_resource_group.auth]
}

module "key_vault" {
  source              = "../../modules/key_vault"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location

  entraid_admins = { "app" : azurerm_user_assigned_identity.application_admin.principal_id }

  tenant_id = var.tenant_id
  dns_zones = [module.dns.zones["key_vault"].id]
  subnet_id = module.vnet.subnets["default"].id

  depends_on = [azurerm_resource_group.auth]
}

module "service_bus" {
  source              = "../../modules/service_bus"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location

  is_prod_like           = var.is_prod_like
  key_vault_id           = module.key_vault.id
  dns_zones              = [module.dns.zones["service_bus"].id]
  subnet_id              = module.vnet.subnets["default"].id
  permitted_ip_addresses = [module.nat.ip]

  depends_on = [azurerm_resource_group.auth, module.key_vault]
}

module "postgres_server" {
  source              = "../../modules/postgres_server"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location
  tenant_id           = var.tenant_id
  is_prod_like        = var.is_prod_like

  entraid_admins = [
    {
      principal_id   = azurerm_user_assigned_identity.application_admin.principal_id
      principal_name = azurerm_user_assigned_identity.application_admin.name
      principal_type = "ServicePrincipal"
    }
  ]

  dns_zone     = module.dns.zones["postgres"].id
  key_vault_id = module.key_vault.id
  subnet_id    = module.vnet.subnets["postgres"].id

  depends_on = [azurerm_resource_group.auth, module.key_vault]
}

module "application_insights" {
  source              = "../../modules/application_insights"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location

  depends_on = [azurerm_resource_group.auth]
}

module "container_app_environment" {
  source              = "../../modules/container_app_environment"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location
  domains             = local.domains

  vnet_id                    = module.vnet.id
  subnet_id                  = module.vnet.subnets["container_apps"].id
  log_analytics_workspace_id = module.application_insights.log_analytics_workspace_id

  depends_on = [azurerm_resource_group.auth, module.key_vault]
}

module "application_gateway" {
  source              = "../../modules/application_gateway"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location

  subnet_id                  = module.vnet.subnets["application_gateway"].id
  log_analytics_workspace_id = module.application_insights.log_analytics_workspace_id

  cert_keyvault_name               = var.cert_keyvault_name
  cert_resource_group_name         = var.cert_resource_group_name
  cert_user_assigned_identity_name = var.cert_user_assigned_identity_name

  domains  = local.domains
  services = var.services

  depends_on = [azurerm_resource_group.auth]
}

module "app_configuration" {
  source              = "../../modules/app_configuration"
  metadata            = local.metadata
  resource_group_name = local.resource_group_name
  location            = var.location

  variables = {
    "Postgres:Host"                        = module.postgres_server.host
    "ServiceBus:Endpoint"                  = module.service_bus.host
    "ApplicationInsights:ConnectionString" = module.application_insights.connection_string,
    "Sentinel"                             = timestamp()
  }

  depends_on = [azurerm_resource_group.auth]
}
