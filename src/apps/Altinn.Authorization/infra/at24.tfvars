environment = "at24"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/4a142a74-1861-42c5-9014-36cc46ed71a0/"
    namespace       = "default"
    service_account = "altinn-authorization"
  }
]

platform_workflow_principal_ids = [
  "d0d7176a-41e5-428b-b272-7ad88bc4939e", # altinn-authorization-app-at24-aks01
  "11c928b2-1ec7-4de0-8486-9b3120544971"  # altinn-authorization-app-at24-aks02
]
