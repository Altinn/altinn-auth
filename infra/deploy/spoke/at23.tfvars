organization                    = "altinn"
product_name                    = "auth"
instance                        = "001"
environment                     = "at23"
single_stack_ipv4_address_space = "10.202.96.0/21"
dual_stack_ipv4_address_space   = "10.202.112.0/20"
dual_stack_ipv6_address_space   = "fd0a:7204:c37f:300::/56"
hub_subscription_id             = "01de49cb-48ef-4494-bc9d-b9e19a90bcd5"
hub_principal_id                = "a9585a64-20f0-4d18-aba6-9930f92b809c"
prod_like                       = false

spoke_principal_ids = ["6eaed23e-df7f-4708-9c8e-a7f34deeadb4"]
service_bus_firewall = [
  "20.100.48.80/31", # platform-at22-01-prefix
  "20.100.48.100/31" # platform-at22-02-prefix
]

appconfiguration = {
  platform_resource_registry_endpoint = "http://altinn-resource-registry.default.svc.cluster.local"
  platform_register_endpoint          = "http://altinn-register.default.svc.cluster.local"
  platform_sbl_bridge_endpoint        = "https://at23.altinn.cloud/sblbridge/"
}
