environment = "tt02"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/d118512b-ed4c-4387-8ef5-c893a1e7b788/"
    namespace       = "default"
    service_account = "altinn-register"
  }
]
deploy_app_principal_id = "a9585a64-20f0-4d18-aba6-9930f92b809c" # GitHub: altinn/altinn-authorization-tmp - Prod
platform_workflow_principal_ids = [
  "e64ffd29-ca41-4b8c-84dc-a71cff5424c1", # altinn-register-app-tt02-aks01
  "0ba88429-f3ff-4079-ba46-a3d3eeb82f9c"  # altinn-register-app-tt02-aks02
]
db_max_pool_size = 10
db_compute_sku   = "D2"
sbl_endpoint     = "https://ai-tt02-vip-sblbridge.ai.basefarm.net/sblbridge/"

features = {
  a2_party_import = {
    parties  = true
    user_ids = true
  }
}
