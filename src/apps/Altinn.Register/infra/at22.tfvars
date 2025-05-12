environment = "at22"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/cb4eb3bd-3ee3-4e48-ad7c-db31c12f5f64/"
    namespace       = "default"
    service_account = "altinn-register"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "9b2c69fa-b718-42cc-8eb6-b969a8238604", # altinn-register-app-at22-aks01
  "2ac064c5-5241-4097-a2d6-a27c077f1f51"  # altinn-register-app-at22-aks02
]
db_max_pool_size = 4
db_compute_tier  = "Burstable"
db_compute_size  = "Standard_B2s"
sbl_endpoint     = "https://at22.altinn.cloud/sblbridge/"

features = {
  a2_party_import = {
    parties  = true
    user_ids = false
  }
}
