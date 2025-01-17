locals {
  vpn_client_id = "c632b3df-fb67-4d84-bdcf-b95ad541b5c8"
}

resource "azuread_application" "vpn" {
  display_name     = "Product: Authorization VPN"
  sign_in_audience = "AzureADMyOrg"

  api {
    oauth2_permission_scope {
      id                         = static_data.static.output.api_id
      admin_consent_description  = "Allow the application to access the VPN API on behalf of the signed-in user."
      admin_consent_display_name = "Access VPN API"
      type                       = "Admin"
      value                      = "VPN.Access"
    }

    requested_access_token_version = 2
  }

  lifecycle {
    ignore_changes = [identifier_uris]
  }

  owners = var.spoke_principals_ids
}

resource "azuread_application_identifier_uri" "vpn" {
  application_id = azuread_application.vpn.id
  identifier_uri = "api://${azuread_application.vpn.client_id}"
}

resource "azuread_application_pre_authorized" "vpn" {
  application_id       = azuread_application.vpn.id
  authorized_client_id = local.vpn_client_id
  permission_ids = [
    static_data.static.output.api_id
  ]
}

resource "azuread_service_principal" "vpn" {
  client_id = azuread_application.vpn.client_id
}

resource "azurerm_public_ip" "vpn" {
  name                = "pipvpn${local.suffix}"
  location            = azurerm_resource_group.hub.location
  resource_group_name = azurerm_resource_group.hub.name
  allocation_method   = "Static"
  public_ip_prefix_id = azurerm_public_ip_prefix.ipv4.id
}

resource "azurerm_virtual_network_gateway" "vpn" {
  name                = "vpngw${local.suffix}"
  location            = azurerm_resource_group.hub.location
  resource_group_name = azurerm_resource_group.hub.name

  type     = "Vpn"
  vpn_type = "RouteBased"
  sku      = "VpnGw1"

  enable_bgp                 = false
  active_active              = false
  dns_forwarding_enabled     = false
  private_ip_address_enabled = false

  ip_configuration {
    name                          = "ip-config"
    public_ip_address_id          = azurerm_public_ip.vpn.id
    private_ip_address_allocation = "Dynamic"
    subnet_id                     = azurerm_subnet.hub["GatewaySubnet"].id
  }

  vpn_client_configuration {
    address_space        = ["192.168.20.0/24"]
    vpn_auth_types       = ["AAD", "Certificate"]
    vpn_client_protocols = ["OpenVPN"]

    root_certificate {
      name             = "VPNRootCert"
      public_cert_data = data.azurerm_key_vault_certificate.vpn.certificate_data_base64
    }

    aad_tenant   = "https://login.microsoftonline.com/${data.azurerm_client_config.current.tenant_id}"
    aad_audience = azuread_application.vpn.client_id
    aad_issuer   = "https://sts.windows.net/${data.azurerm_client_config.current.tenant_id}/"
  }
}

resource "azurerm_route_table" "vpn" {
  name                = "rtvpn${local.suffix}"
  resource_group_name = azurerm_resource_group.hub.name
  location            = azurerm_resource_group.hub.location

  route = [
    {
      name                   = "IPv4ForcedTunneling1"
      address_prefix         = "0.0.0.0/1"
      next_hop_type          = "VirtualAppliance"
      next_hop_in_ip_address = azurerm_firewall.firewall.ip_configuration[0].private_ip_address
    },
    {
      name                   = "IPv4ForcedTunneling2"
      address_prefix         = "128.0.0.0/1"
      next_hop_type          = "VirtualAppliance"
      next_hop_in_ip_address = azurerm_firewall.firewall.ip_configuration[0].private_ip_address
    }
  ]
}

resource "azurerm_subnet_route_table_association" "vpn" {
  route_table_id = azurerm_route_table.vpn.id
  subnet_id      = azurerm_subnet.hub["GatewaySubnet"].id
}

resource "azurerm_key_vault_certificate" "vpn" {
  name         = "VPNRootCert"
  key_vault_id = azurerm_key_vault.key_vault.id

  certificate {
    contents = base64encode("${tls_self_signed_cert.root.cert_pem}\n${tls_private_key.root.private_key_pem_pkcs8}")
  }

  depends_on = [
    azurerm_role_assignment.key_vault_administrator
  ]
}

data "azurerm_key_vault_certificate" "vpn" {
  name         = "VPNRootCert"
  key_vault_id = azurerm_key_vault.key_vault.id
  depends_on   = [azurerm_key_vault_certificate.vpn]
}

resource "azurerm_storage_container" "certs" {
  name                  = "vpncerts"
  storage_account_id    = azurerm_storage_account.storage.id
  container_access_type = "private"
}

resource "tls_private_key" "root" {
  algorithm = "RSA"
  rsa_bits  = 2048
}

resource "tls_private_key" "client" {
  algorithm = "RSA"
  rsa_bits  = 2048

  for_each = toset(var.client_certs)
}

resource "tls_self_signed_cert" "root" {
  private_key_pem      = tls_private_key.root.private_key_pem
  is_ca_certificate    = true
  set_authority_key_id = true
  set_subject_key_id   = true

  validity_period_hours = 87600 # 1 year
  allowed_uses          = []

  subject {
    common_name = "VPN CA"
  }
}

resource "tls_cert_request" "client" {
  private_key_pem = tls_private_key.client[each.key].private_key_pem
  dns_names       = [each.value]

  subject {
    common_name = each.value
  }

  for_each = toset(var.client_certs)
}

resource "tls_locally_signed_cert" "client" {
  cert_request_pem = tls_cert_request.client[each.key].cert_request_pem

  ca_cert_pem        = tls_self_signed_cert.root.cert_pem
  ca_private_key_pem = tls_private_key.root.private_key_pem

  validity_period_hours = 8760 # 1 year
  allowed_uses          = ["client_auth"]
  set_subject_key_id    = true

  for_each = toset(var.client_certs)
}

//
// Need to fetch VPN cert
resource "pkcs12_from_pem" "client_certs" {
  password = ""
  cert_pem = tls_locally_signed_cert.client[each.key].cert_pem

  private_key_pem  = tls_private_key.client[each.key].private_key_pem
  private_key_pass = ""

  ca_pem   = tls_self_signed_cert.root.cert_pem
  for_each = toset(var.client_certs)
}

resource "azurerm_storage_blob" "ca_pem_cert" {
  name                   = "CaCert.pem"
  storage_container_name = azurerm_storage_container.certs.name
  storage_account_name   = azurerm_storage_account.storage.name

  access_tier    = "Cool"
  type           = "Block"
  source_content = tls_self_signed_cert.root.cert_pem
}

resource "azurerm_storage_blob" "client_pem_cert" {
  name                   = "${each.value}/${each.value}Cert.pem"
  storage_container_name = azurerm_storage_container.certs.name
  storage_account_name   = azurerm_storage_account.storage.name

  access_tier    = "Cool"
  type           = "Block"
  source_content = tls_locally_signed_cert.client[each.key].cert_pem

  for_each = toset(var.client_certs)
}

resource "azurerm_storage_blob" "client_key_cert" {
  name                   = "${each.value}/${each.value}Key.pem"
  storage_container_name = azurerm_storage_container.certs.name
  storage_account_name   = azurerm_storage_account.storage.name

  access_tier    = "Cool"
  type           = "Block"
  source_content = tls_private_key.client[each.value].private_key_pem

  for_each = toset(var.client_certs)
}

resource "azurerm_storage_blob" "client_pfx_cert" {
  name                   = "${each.value}/${each.value}Cert.pfx"
  storage_container_name = azurerm_storage_container.certs.name
  storage_account_name   = azurerm_storage_account.storage.name

  access_tier    = "Cool"
  type           = "Block"
  source_content = pkcs12_from_pem.client_certs[each.key].result
  for_each       = toset(var.client_certs)
}
