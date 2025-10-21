organization                    = "altinn"
product_name                    = "auth"
instance                        = "001"
single_stack_ipv4_address_space = "10.202.0.0/20"
dual_stack_ipv4_address_space   = "10.202.16.0/20"
dual_stack_ipv6_address_space   = "fd0a:7204:c37f::/56"
client_certs = [
  "remilovoll",
  "jonkjetil",
  "mariusthuen",
  "andreasisnes",
  "havardandersen",
  "aleksanderh",
  "github",
]
hub_principal_ids = [
  "a9585a64-20f0-4d18-aba6-9930f92b809c" # App: GitHub: altinn/altinn-authorization-tmp - Prod
]
spoke_principal_ids = [
  "6eaed23e-df7f-4708-9c8e-a7f34deeadb4" # App: GitHub: altinn/altinn-authorization-tmp - Dev
]
maintainers_principal_ids = [
  "93bed750-6ca4-47ae-ac43-b45fff4930f6", # Group: Altinn Product Authorization: Admins Dev
  "3863fbc0-a24b-42bf-af3d-f45111814457", # Group: Altinn Product Authorization: Admins Prod
  "a9585a64-20f0-4d18-aba6-9930f92b809c", # App: GitHub: altinn/altinn-authorization-tmp - Prod
  "48587eaa-8f33-43ed-a0c3-108c3681e84b", # User: Nilsen, Andreas Isnes (ai-prod) -- NOTE: Temporarily
  "be1a510a-db1e-473c-a73a-558cdb68e353"  # User: Nilsen, Andreas Isnes (ai-dev)  -- NOTE: Temporarily
]
developer_dev_principal_ids = [
  "6d54df21-3547-41a2-8d0d-529fad054807" # Group: Altinn Product Authorization: Developers Dev
]
developer_prod_principal_ids = [
  "c410f062-def4-44f5-9a45-b23ddcdd57c3" # Group: Altinn Product Authorization: Developers Prod
]
vpn_owners_principal_ids = [
  "6eaed23e-df7f-4708-9c8e-a7f34deeadb4", # App: GitHub: altinn/altinn-authorization-tmp - Dev
  "a9585a64-20f0-4d18-aba6-9930f92b809c"  # App: GitHub: altinn/altinn-authorization-tmp - Prod
]
vpn_routes = {
  AuthorizationInfrastructure = [
    "10.202.0.0/16"
  ],

  # Use  "name": "ServiceBus.NorwayEast"
  # https://www.microsoft.com/en-us/download/details.aspx?id=56519"
  "ServiceBusNoEast" = [
    "51.13.0.128/26",
    "51.120.76.34/32",
    "51.120.83.200/32",
    "51.120.98.16/29",
    "51.120.106.128/29",
    "51.120.109.208/28",
    "51.120.210.128/29",
    "51.120.213.48/28",
    "51.120.237.64/26",
    # "2603:1020:e04:1::220/123",
    # "2603:1020:e04:3::500/120",
    # "2603:1020:e04:402::170/125",
    # "2603:1020:e04:802::150/125",
    # "2603:1020:e04:c02::150/125"
  ]
}
