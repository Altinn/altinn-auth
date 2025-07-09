environment = "yt01"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/6c3cbd25-2f6e-4003-b073-a635b8d0a1b1/"
    namespace       = "default"
    service_account = "altinn-access-management"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "05ca3a17-bfc2-432e-aea2-81176c47f176", # altinn-access-management-app-yt01-aks01
  "0abd3bf5-4c0c-432a-8475-f977adc816c8"  # altinn-access-management-app-yt01-aks02
]
db_max_pool_size = 10
db_compute_tier  = "GeneralPurpose"
db_compute_size  = "Standard_D2ads_v5"
