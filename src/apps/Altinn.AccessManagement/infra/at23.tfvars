environment = "at23"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/68c4e940-5ccb-4825-9eb5-2bb429e242b5/"
    namespace       = "default"
    service_account = "altinn-access-management"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "626d6e0e-278c-420b-8100-cf6f818ea601", # altinn-access-management-app-at23-aks01
  "5ba57db2-e173-4e07-9693-427a4b91d00b"  # altinn-access-management-app-at23-aks02
]
db_max_pool_size = 4
db_compute_sku   = "D2"
