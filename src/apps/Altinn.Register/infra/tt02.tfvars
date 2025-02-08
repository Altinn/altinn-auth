environment = "tt02"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/d118512b-ed4c-4387-8ef5-c893a1e7b788/"
    namespace       = "default"
    service_account = "altinn-register"
  }
]
deploy_app_principal_id = "a9585a64-20f0-4d18-aba6-9930f92b809c" # GitHub: altinn/altinn-authorization-tmp - Prod
