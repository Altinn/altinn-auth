environment = "at24"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/4a142a74-1861-42c5-9014-36cc46ed71a0/"
    namespace       = "default"
    service_account = "altinn-access-management"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "226b2bbc-a0bd-4cdc-9d66-27822e900f71", # altinn-access-management-app-at24-aks01
  "23120df0-6945-49e6-b62e-3c3a797e5c51"  # altinn-access-management-app-at24-aks02
]
db_max_pool_size = 4
