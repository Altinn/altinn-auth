locals {
  container_app_name                  = "container_app"
  container_app_backend_settings_name = "${local.container_app_name}_container_app"
  container_app_frontend_port_name    = "${local.container_app_name}_frontend_port"
  container_app_frontend_ip_name      = "${local.container_app_name}_frontend_ip"

  priority = {
    api      = 1
    frontend = 2
  }
}

resource "azurerm_public_ip" "ingress" {
  name                 = "pipingress${var.metadata.suffix}"
  resource_group_name  = var.resource_group_name
  location             = var.location
  sku                  = "Standard"
  allocation_method    = "Static"
  ip_version           = "IPv4"
  ddos_protection_mode = "VirtualNetworkInherited"
  zones                = var.zones
}

data "azurerm_key_vault" "cert" {
  name                = var.cert_keyvault_name
  resource_group_name = var.cert_resource_group_name
}

data "azurerm_user_assigned_identity" "cert" {
  name                = var.cert_user_assigned_identity_name
  resource_group_name = var.cert_resource_group_name
}

data "azurerm_key_vault_certificate" "cert" {
  name         = "cert"
  key_vault_id = data.azurerm_key_vault.cert.id
}

resource "azurerm_application_gateway" "appgw" {
  name                = "appgw${var.metadata.suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  identity {
    type         = "UserAssigned"
    identity_ids = [data.azurerm_user_assigned_identity.cert.id]
  }

  zones = var.zones

  sku {
    name = "Standard_v2"
    tier = "Standard_v2"
  }

  gateway_ip_configuration {
    name      = "gateway_subnet"
    subnet_id = var.subnet_id
  }

  autoscale_configuration {
    min_capacity = 0
    max_capacity = var.max_capacity
  }

  ssl_certificate {
    name                = "ssl_certificate"
    key_vault_secret_id = data.azurerm_key_vault_certificate.cert.versionless_secret_id
  }

  frontend_port {
    name = local.container_app_frontend_port_name
    port = 443
  }

  frontend_ip_configuration {
    name                 = local.container_app_frontend_ip_name
    public_ip_address_id = azurerm_public_ip.ingress.id
  }

  backend_address_pool {
    name  = "backend_address_pool_container_app_index"
    fqdns = ["index.${var.domains["frontend"]}"]
  }

  backend_http_settings {
    name                                = "backend_http_settings_container_app_index"
    cookie_based_affinity               = "Disabled"
    path                                = ""
    port                                = 80
    protocol                            = "Http"
    probe_name                          = "probe_container_app_index"
    pick_host_name_from_backend_address = true
  }

  probe {
    name                                      = "probe_container_app_index"
    protocol                                  = "Http"
    path                                      = "/"
    pick_host_name_from_backend_http_settings = true
    interval                                  = 30
    timeout                                   = 30
    unhealthy_threshold                       = 3
    match {
      status_code = ["200"]
    }
  }

  dynamic "backend_address_pool" {
    content {
      name  = "backend_address_pool_container_app_${backend_address_pool.value.domain}_${backend_address_pool.value.hostname}"
      fqdns = ["${backend_address_pool.value.hostname}.${var.domains[backend_address_pool.value.domain]}"]
    }

    for_each = toset(var.services)
  }

  dynamic "probe" {
    content {
      name                                      = "probe_container_app_${probe.value.domain}_${probe.value.hostname}"
      protocol                                  = "Http"
      path                                      = "/healthz"
      pick_host_name_from_backend_http_settings = true
      interval                                  = 30
      timeout                                   = 30
      unhealthy_threshold                       = 3
      match {
        status_code = ["200"]
      }
    }

    for_each = toset(var.services)
  }

  # Container Apps
  dynamic "request_routing_rule" {
    content {
      name               = "request_routing_rule_container_${request_routing_rule.key}"
      http_listener_name = "http_listener_container_app_${request_routing_rule.key}"
      url_path_map_name  = "url_path_map_container_app_${request_routing_rule.key}"
      priority           = local.priority[request_routing_rule.key]
      rule_type          = "PathBasedRouting"
    }

    for_each = var.domains
  }

  dynamic "url_path_map" {
    content {
      name                               = "url_path_map_container_app_${url_path_map.key}"
      default_backend_address_pool_name  = "backend_address_pool_container_app_index"
      default_backend_http_settings_name = "backend_http_settings_container_app_index"
      dynamic "path_rule" {
        content {
          name                       = "path_rule_container_app_${path_rule.value.domain}_${path_rule.value.hostname}"
          backend_address_pool_name  = "backend_address_pool_container_app_${path_rule.value.domain}_${path_rule.value.hostname}"
          backend_http_settings_name = "backend_http_settings_container_app_${path_rule.value.domain}_${path_rule.value.hostname}"
          paths                      = path_rule.value.path == "/" ? ["/*"] : ["${path_rule.value.path}/*", path_rule.value.path]
        }

        for_each = { for service in var.services : service.hostname => service if url_path_map.key == service.domain }
      }
    }

    for_each = var.domains
  }

  dynamic "backend_http_settings" {
    content {
      name                                = "backend_http_settings_container_app_${backend_http_settings.value.domain}_${backend_http_settings.value.hostname}"
      cookie_based_affinity               = "Disabled"
      path                                = ""
      port                                = 80
      protocol                            = "Http"
      probe_name                          = "probe_container_app_${backend_http_settings.value.domain}_${backend_http_settings.value.hostname}"
      pick_host_name_from_backend_address = true
    }

    for_each = toset(var.services)
  }


  dynamic "http_listener" {
    content {
      name                           = "http_listener_container_app_${http_listener.key}"
      frontend_ip_configuration_name = local.container_app_frontend_ip_name
      frontend_port_name             = local.container_app_frontend_port_name
      protocol                       = "Https"
      host_name                      = http_listener.value
      ssl_certificate_name           = "ssl_certificate"
    }

    for_each = var.domains
  }


  tags = var.metadata
  lifecycle {
    prevent_destroy = true
    ignore_changes  = [tags]
  }
}

resource "azurerm_monitor_diagnostic_setting" "diagnostics" {
  name = "log_analytics_workspace"

  target_resource_id         = azurerm_application_gateway.appgw.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category_group = "allLogs"
  }

  enabled_metric {
    category = "AllMetrics"
  }
}
