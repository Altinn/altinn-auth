organization                    = "altinn"
product_name                    = "auth"
instance                        = "001"
environment                     = "yt01"
single_stack_ipv4_address_space = "10.202.160.0/21"
dual_stack_ipv4_address_space   = "10.202.176.0/20"
dual_stack_ipv6_address_space   = "fd0a:7204:c37f:500::/56"
hub_subscription_id             = "01de49cb-48ef-4494-bc9d-b9e19a90bcd5"
hub_principal_id                = "a9585a64-20f0-4d18-aba6-9930f92b809c"
prod_like                       = true

spoke_principal_ids = ["6eaed23e-df7f-4708-9c8e-a7f34deeadb4"]
service_bus_firewall = [
  "51.13.29.120/31",
  "51.13.29.32/31"
]

maintainers_principal_ids = [
  "6d54df21-3547-41a2-8d0d-529fad054807", # Group: Altinn Product Authorization: Developers Dev
  "93bed750-6ca4-47ae-ac43-b45fff4930f6", # Group: Altinn Product Authorization: Admins Dev
]

appconfiguration = {
  platform_notifications_endpoint     = "http://altinn-notifications.default.svc.cluster.local"
  platform_resource_registry_endpoint = "http://altinn-resource-registry.default.svc.cluster.local"
  platform_register_endpoint          = "http://altinn-register.default.svc.cluster.local"
  platform_accessmanagement_endpoint  = "http://altinn-access-management.default.svc.cluster.local"
  platform_sbl_bridge_endpoint        = "https://ai-yt01-vip-sblbridge.ai.basefarm.net/sblbridge/"
  maskinporten_endpoint               = "https://test.maskinporten.no/"
}

services = {
  altinn2 = {
    host = "yt01.ai.basefarm.net"
  }

  altinn-authentication = {
    protocol = "http"
    host     = "altinn-authentication.default.svc.cluster.local"
  }

  altinn-access-management = {
    protocol = "http"
    host     = "altinn-access-management.default.svc.cluster.local"
  }

  altinn-resource-registry = {
    protocol = "http"
    host     = "altinn-resource-registry.default.svc.cluster.local"
  }

  altinn-register = {
    protocol = "http"
    host     = "altinn-register.default.svc.cluster.local"
  }

  folkeregisteret = {
    host = "folkeregisteret-api-konsument.sits.no"
  }

  sire = {
    host = "skatteetatenregistrertselskap.api.skatteetaten-test.no"
  }

  sire-events = {
    host = "skatteetatenregistrertselskaphendelser.api.skatteetaten-test.no"
  }
}

logging = {
  min_level = {
    "AltinnCore.Authentication" = "Warning"

    "Microsoft" = "Warning"
    "System"    = "Warning"
    "Polly"     = "Warning"
    "Npgsql"    = "Warning"
  }
}
