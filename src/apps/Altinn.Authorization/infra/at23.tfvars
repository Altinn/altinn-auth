environment = "at23"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/68c4e940-5ccb-4825-9eb5-2bb429e242b5/"
    namespace       = "default"
    service_account = "altinn-authorization"
  }
]

platform_workflow_principal_ids = [
  "8cb9ad7d-7082-41c0-8a63-8bb362ce9d90", # altinn-authorization-app-at23-aks01
  "7bf2d553-b1fc-47a5-a4c1-1bb5c7dfb5d4"  # altinn-authorization-app-at23-aks02
]
