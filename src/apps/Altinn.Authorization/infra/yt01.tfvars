environment = "yt01"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/6c3cbd25-2f6e-4003-b073-a635b8d0a1b1/"
    namespace       = "default"
    service_account = "altinn-authorization"
  }
]
platform_workflow_principal_ids = [
  "23a25a43-67b3-454e-b6a9-fe782b3bfa95", # altinn-authorization-app-yt01-aks01
  "509555cc-d8ff-4db3-8726-c83fa9b746e2"  # altinn-authorization-app-yt01-aks02
]
