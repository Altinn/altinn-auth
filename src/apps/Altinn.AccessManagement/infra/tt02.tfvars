environment = "tt02"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/d118512b-ed4c-4387-8ef5-c893a1e7b788/"
    namespace       = "default"
    service_account = "altinn-access-management"
  }
]
deploy_app_principal_id = "a9585a64-20f0-4d18-aba6-9930f92b809c" # GitHub: altinn/altinn-authorization-tmp - Prod
platform_workflow_principal_ids = [
  "1f5bc37d-3a69-4fbb-b2f2-c1da190cd0d2", # altinn-access-management-app-tt02-aks01
  "7df20147-fff7-4c9b-a28d-07a0005687d3"  # altinn-access-management-app-tt02-aks02
]
db_max_pool_size = 10
db_compute_tier  = "GeneralPurpose"
db_compute_size  = "Standard_D2s_v3"
