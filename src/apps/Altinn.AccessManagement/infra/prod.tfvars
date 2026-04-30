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
db_max_pool_size = 10
db_compute_sku   = "D2"
configuration = {
  consent = {
    batch_size                = 5000,
    max_degree_of_parallelism = 10
  }
  core = {
    request_notify_request_approved_in_seconds = 600
    request_notify_request_pending_in_seconds  = 960
    notifications = {
      access_added_notify_in_seconds        = 960
      access_removed_notify_in_seconds      = 600
      request_pending_notify_in_seconds     = 960
      request_reviewed_notify_in_seconds    = 600
      rightholder_added_notify_in_seconds   = 120
      rightholder_removed_notify_in_seconds = 120
    }
  }
}
