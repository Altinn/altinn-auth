variable "organization" {
  type    = string
  default = "altinn"
}

variable "product_name" {
  type    = string
  default = "auth"
}

variable "name" {
  type    = string
  default = "register"
}

variable "instance" {
  type    = string
  default = "001"
}

variable "environment" {
  type = string
}

variable "hub_subscription_id" {
  type    = string
  default = "01de49cb-48ef-4494-bc9d-b9e19a90bcd5"
}

variable "deploy_app_principal_id" {
  type = string
}

variable "aks_federation" {
  type = list(object({
    issuer_url      = string
    namespace       = string
    service_account = string
  }))
}

variable "platform_workflow_principal_ids" {
  type    = list(string)
  default = []
}

variable "db_max_pool_size" {
  type    = number
  default = 4
}

variable "db_compute_sku" {
  type = string
}

variable "db_storage_tier" {
  type    = string
  default = "P10"
}

variable "sbl_endpoint" {
  type = string
}

variable "use_pgbouncer" {
  type    = bool
  default = false
}

variable "enable_high_availability" {
  type    = bool
  default = false
}

variable "key_vault_rbac" {
  type = list(object({
    id       = string
    rolename = string
  }))

  default = []
}

variable "features" {
  type = object({
    a2_party_import = optional(object({
      parties  = optional(bool, false),
      user_ids = optional(bool, false),
      profiles = optional(bool, false),
    }), {})
    party_import = optional(object({
      system_users = optional(bool, false),
    }), {})
  })
  default = {}
}

variable "config" {
  type = object({
    a2_party_import = optional(object({
      max_db_size_in_gib = optional(number, 20),
    }), {})
  })
  default = {}
}
