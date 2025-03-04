environment = "at22"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/cb4eb3bd-3ee3-4e48-ad7c-db31c12f5f64/"
    namespace       = "default"
    service_account = "altinn-access-management"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "8fcf3019-ee5f-44d4-8c57-b2248f6f282e", # altinn-access-management-app-at22-aks01
  "2b74d490-2e91-48ad-aee1-478276b7e4e4"  # altinn-access-management-app-at22-aks02
]
pg_dns_hex = "c0d5763dc925"
