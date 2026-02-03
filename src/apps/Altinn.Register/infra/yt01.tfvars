environment = "yt01"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/6c3cbd25-2f6e-4003-b073-a635b8d0a1b1/"
    namespace       = "default"
    service_account = "altinn-register"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "b367c518-e46e-4dba-bd16-458eb1334163", # altinn-register-app-yt01-aks01
  "14e02a1e-3292-430e-ae4f-855ed40847fd"  # altinn-register-app-yt01-aks02
]
db_max_pool_size = 100
db_compute_sku   = "D4"
db_storage_tier  = "P40"
sbl_endpoint     = "https://ai-yt01-vip-sblbridge.ai.basefarm.net/sblbridge/"

features = {
  maskinporten = true
  a2_party_import = {
    parties  = true
    user_ids = true
    profiles = true
  }
  party_import = {
    system_users = true
  }
}

config = {
  a2_party_import = {
    max_db_size_in_gib = 100
  }

  maskinporten = {
    client_id = "6b3069e2-bc65-42ce-9aab-413e405dd5fe"
    scope     = "folkeregister:deling/offentligmedhjemmel"
  }
}
