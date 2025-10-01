environment = "at24"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/4a142a74-1861-42c5-9014-36cc46ed71a0/"
    namespace       = "default"
    service_account = "altinn-register"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "3c6f503e-1dd3-4db2-bbf0-6264dd0390f9", # altinn-register-app-at24-aks01
  "3e2be4e1-e4ea-4cdd-8e3e-62fc06f2eca1"  # altinn-register-app-at24-aks02
]
db_max_pool_size = 4
db_compute_sku   = "D2"
sbl_endpoint     = "https://at24.altinn.cloud/sblbridge/"
use_pgbouncer    = true

features = {
  a2_party_import = {
    parties  = true
    user_ids = true
    profiles = true
  }
  party_import = {
    system_users = true
  }
}
