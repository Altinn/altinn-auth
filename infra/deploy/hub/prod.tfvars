organization                    = "altinn"
product_name                    = "auth"
instance                        = "001"
single_stack_ipv4_address_space = "10.202.0.0/20"
dual_stack_ipv4_address_space   = "10.202.16.0/20"
dual_stack_ipv6_address_space   = "fd0a:7204:c37f::/56"
client_certs = [
  "jonkjetil",
  "mariusthuen",
  "andreasisnes",
  "github",
]
hub_principal_ids = [
  "a9585a64-20f0-4d18-aba6-9930f92b809c" # GitHub: altinn/altinn-authorization-tmp - Prod
]
spoke_principal_ids = [
  "6eaed23e-df7f-4708-9c8e-a7f34deeadb4" # GitHub: altinn/altinn-authorization-tmp - Dev
]
maintainers_principal_ids = [
  "3863fbc0-a24b-42bf-af3d-f45111814457", # Altinn Product Authorization: Admins Prod
  "48587eaa-8f33-43ed-a0c3-108c3681e84b", # ai-prod
  "be1a510a-db1e-473c-a73a-558cdb68e353", # ai-dev
  "a9585a64-20f0-4d18-aba6-9930f92b809c"  # GitHub: altinn/altinn-authorization-tmp - Prod
]
developer_dev_principal_ids = [
  "6d54df21-3547-41a2-8d0d-529fad054807" # Altinn Product Authorization: Developers Dev
]
developer_prod_principal_ids = [
  "c410f062-def4-44f5-9a45-b23ddcdd57c3" # Altinn Product Authorization: Developers Prod
]
vpn_owners_principal_ids = [
  "6eaed23e-df7f-4708-9c8e-a7f34deeadb4", # GitHub: altinn/altinn-authorization-tmp - Dev
  "a9585a64-20f0-4d18-aba6-9930f92b809c"  # GitHub: altinn/altinn-authorization-tmp - Prod
]
