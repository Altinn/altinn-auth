environment = "prod"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/f61fe162-dabf-4796-b3e3-694a0503ffa2/"
    namespace       = "default"
    service_account = "altinn-access-management"
  }
]
platform_workflow_principal_ids = [
  "3edcb10a-40dd-48f8-81f0-eab283ccc11c", # altinn-authorization-app-prod-aks01
  "b1d1bb7f-e131-46fc-9900-ba8c20db6590"  # altinn-authorization-app-prod-aks02
]
