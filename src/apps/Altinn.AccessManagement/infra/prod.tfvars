environment = "prod"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/f61fe162-dabf-4796-b3e3-694a0503ffa2/"
    namespace       = "default"
    service_account = "altinn-access-management"
  }
]
deploy_app_principal_id = "a9585a64-20f0-4d18-aba6-9930f92b809c" # GitHub: altinn/altinn-authorization-tmp - Prod
platform_workflow_principal_ids = [
  "ba5eff20-2b61-42cd-8cfc-fe2c78f4e7f6", # altinn-access-management-app-prod-aks01
  "f4773063-9788-45a9-8e53-57f908e1566a"  # altinn-access-management-app-prod-aks02
]
pg_dns_hex = "cf8a4481e1c1"
