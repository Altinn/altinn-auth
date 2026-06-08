environment = "at23"
aks_federation = [
  {
    issuer_url      = "https://norwayeast.oic.prod-aks.azure.com/cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c/68c4e940-5ccb-4825-9eb5-2bb429e242b5/"
    namespace       = "default"
    service_account = "altinn-register"
  }
]
deploy_app_principal_id = "6eaed23e-df7f-4708-9c8e-a7f34deeadb4"
platform_workflow_principal_ids = [
  "143d9d84-6ef6-4c60-a2a5-9cb232dd2268", # altinn-register-app-at23-aks01
  "e785218c-e5a5-4c8d-b1df-f262b8eaa241"  # altinn-register-app-at23-aks02
]
db_max_pool_size = 50
db_compute_sku   = "D2"
sbl_endpoint     = "https://at23.altinn.cloud/sblbridge/"
use_pgbouncer    = true
key_vault_rbac = [{
  id       = "93bed750-6ca4-47ae-ac43-b45fff4930f6", # Group: Altinn Product Authorization: Admins Dev
  rolename = "Key Vault Secrets Officer"
}]

features = {
  maskinporten = true

  party_import = {
    system_users = true

    npr = {
      enable        = true
      guardianships = true
    }

    sire = {
      enable = true
      listen = true
    }
  }

  ccr_proxy = {
    enable  = true
    record  = true
    process = true
  }
}

config = {
  maskinporten = {
    client_id = "6b3069e2-bc65-42ce-9aab-413e405dd5fe"
    scope     = "folkeregister:deling/offentligmedhjemmel skatteetaten:skatteetatenregistrertselskap"
  }

  api_source = {
    default = "db"
  }

  ccr = {
    federate = {
      enable = true
      source = {
        queue  = "ccr-updates-at23"
        poison = "ccr-updates-at23-poison"
      }
    }

    flatfiles = {
      enable = true
      local = {
        user      = "ccrflatfilesat23"
        container = "ccr-flatfiles-at23"
      }
    }

    clients = {
      e2e-test-at23 = {
        password = "ccr-e2e-test-hash"
        networks = ["0.0.0.0/0", "::/0"]
      }
    }
  }
}
