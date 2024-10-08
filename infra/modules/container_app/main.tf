locals {
  infrastructure_suffix              = "${var.infrastructure_name}${var.instance}${var.environment}"
  infrastructure_resource_group_name = "rg${local.infrastructure_suffix}"
  suffix                             = "${var.name}${var.instance}${var.environment}"

  hostname = lower(var.name)
  domains = {
    "at21" : "auth.at21.altinn.cloud"
    "at22" : "auth.at22.altinn.cloud"
    "at23" : "auth.at23.altinn.cloud"
    "at24" : "auth.at24.altinn.cloud"
    "at25" : "auth.at25.altinn.cloud"
    "tt02" : "auth.tt02.altinn.no"
    "prod" : "auth.altinn.no"
  }
}

data "azurerm_container_app_environment" "caenv" {
  name                = "caenv${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_app_configuration" "appconf" {
  name                = "appconfaltinn${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_servicebus_namespace" "sb" {
  name                = "sbaltinn${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_key_vault" "kv" {
  name                = "kvaltinn${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

resource "azurerm_resource_group" "rg" {
  name     = "rg${local.suffix}"
  location = var.location
}

resource "azurerm_app_configuration_key" "variables" {
  configuration_store_id = data.azurerm_app_configuration.appconf.id
  key                    = each.key
  value                  = each.value
  type                   = "kv"
  label                  = var.name

  for_each = var.app_configuration_variables
}

resource "azurerm_user_assigned_identity" "app" {
  name                = "mi${local.suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
}

# Assign 'AcrPull' role to the container app's managed identity so it can pull images from the ACR
# Assign 'App Configuration Data Reader' role to container app's managed identity so it can read from app configuration
resource "azurerm_role_assignment" "rbac" {
  principal_id         = azurerm_user_assigned_identity.app.principal_id
  role_definition_name = each.value.role_definition_name
  scope                = each.value.scope
  for_each = { for arm in [
    {
      id                   = "service_bus_mass_transit"
      scope                = data.azurerm_servicebus_namespace.sb.id
      role_definition_name = "Azure Service Bus Mass Transit"
      should_assign        = var.can_use_service_bus
    },
    {
      id                   = "app_configuration"
      scope                = data.azurerm_app_configuration.appconf.id
      role_definition_name = "App Configuration Data Reader"
      should_assign        = true
    },
    {
      id                   = "key_vault"
      scope                = data.azurerm_key_vault.kv.id
      role_definition_name = "Key Vault Secrets User"
      should_assign        = true
    }
  ] : arm.id => arm if try(arm.should_assign, false) }
}

data "azurerm_postgresql_flexible_server" "server" {
  name                = "psqlsrvaltinn${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

data "azurerm_user_assigned_identity" "postgres_admin" {
  name                = "mipsqlsrvadmin${local.infrastructure_suffix}"
  resource_group_name = local.infrastructure_resource_group_name
}

resource "azurerm_container_app" "app" {
  name = "ca${local.suffix}"

  container_app_environment_id = data.azurerm_container_app_environment.caenv.id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode                = "Single"
  workload_profile_name        = "basic"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id]
  }

  ingress {
    allow_insecure_connections = true
    external_enabled           = true
    target_port                = 8080
    transport                  = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 2
    max_replicas = var.max_replicas

    container {
      env {
        name  = "EntraId__Identities__Service__ClientId"
        value = azurerm_user_assigned_identity.app.client_id
      }
      env {
        name  = "AppConfiguration__Endpoint"
        value = data.azurerm_app_configuration.appconf.endpoint
      }

      name  = var.name
      image = var.image

      cpu    = var.alloacated_cpu
      memory = "${var.allocated_memory}Gi"
    }
  }

  depends_on = [azurerm_role_assignment.rbac, azurerm_app_configuration_key.variables]
}

resource "azurerm_private_dns_a_record" "record" {
  name                = var.name
  zone_name           = local.domains[var.environment]
  resource_group_name = local.infrastructure_resource_group_name
  ttl                 = 3600
  records             = [data.azurerm_container_app_environment.caenv.static_ip_address]
}

resource "azurerm_container_app_custom_domain" "domain" {
  name                     = "${local.hostname}.${local.domains[var.environment]}"
  certificate_binding_type = "Disabled"
  container_app_id         = azurerm_container_app.app.id
}

