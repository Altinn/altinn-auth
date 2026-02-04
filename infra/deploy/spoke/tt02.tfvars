organization                    = "altinn"
product_name                    = "auth"
instance                        = "001"
environment                     = "tt02"
single_stack_ipv4_address_space = "10.202.192.0/21"
dual_stack_ipv4_address_space   = "10.202.208.0/20"
dual_stack_ipv6_address_space   = "fd0a:7204:c37f:600::/56"
hub_subscription_id             = "01de49cb-48ef-4494-bc9d-b9e19a90bcd5"
hub_principal_id                = "a9585a64-20f0-4d18-aba6-9930f92b809c"
prod_like                       = true

spoke_principal_ids = ["a9585a64-20f0-4d18-aba6-9930f92b809c"]
service_bus_firewall = [
  "20.100.48.118/31",
  "20.100.48.120/31"
]

appconfiguration = {
  platform_resource_registry_endpoint = "http://altinn-resource-registry.default.svc.cluster.local"
  platform_register_endpoint          = "http://altinn-register.default.svc.cluster.local"
  platform_sbl_bridge_endpoint        = "https://ai-tt02-vip-sblbridge.ai.basefarm.net/sblbridge/"
  maskinporten_endpoint               = "https://test.maskinporten.no/"
}

services = {
  altinn-authentication = {
    protocol = "http"
    host     = "altinn-authentication.default.svc.cluster.local"
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
