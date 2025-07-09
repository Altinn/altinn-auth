environment = "prod"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/f61fe162-dabf-4796-b3e3-694a0503ffa2/"
    namespace       = "default"
    service_account = "altinn-register"
  }
]
deploy_app_principal_id = "a9585a64-20f0-4d18-aba6-9930f92b809c" # GitHub: altinn/altinn-authorization-tmp - Prod
platform_workflow_principal_ids = [
  "e7e7ed66-8178-4737-8196-0b2604811939", # altinn-register-app-prod-aks01
  "8a86d76e-91b1-4e47-a411-e81f8e689882"  # altinn-register-app-prod-aks02
]
db_max_pool_size = 10
db_compute_tier  = "GeneralPurpose"
db_compute_size  = "Standard_D8ads_v5"
db_storage_tier  = "P15"
sbl_endpoint     = "https://ai-pr-vip-sblbridge.ai.basefarm.net/sblbridge/"

features = {
  a2_party_import = {
    parties  = true
    user_ids = true
  }
}
