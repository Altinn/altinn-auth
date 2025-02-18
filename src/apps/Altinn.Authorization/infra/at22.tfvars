environment = "at22"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/cb4eb3bd-3ee3-4e48-ad7c-db31c12f5f64/"
    namespace       = "default"
    service_account = "altinn-authorization"
  }
]

platform_workflow_principal_ids = [
  "26502852-da56-4d82-8b25-66acb2929499", # altinn-authorization-app-at22-aks01
  "076c5497-a532-49f0-9bb4-6931ebe70e2a"  # altinn-authorization-app-at22-aks02
]
