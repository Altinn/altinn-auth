environment = "tt02"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/d118512b-ed4c-4387-8ef5-c893a1e7b788/"
    namespace       = "default"
    service_account = "altinn-authorization"
  }
]
platform_workflow_principal_ids = [
  "9e0478d9-3a2b-40f4-a26c-d279dcad3ad7", # altinn-authorization-app-tt02-aks01
  "060f24d3-0951-4a73-b78c-bede03a527e9"  # altinn-authorization-app-tt02-aks02
]
