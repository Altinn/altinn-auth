
variable "metadata" {
  type = object({
    name        = string
    environment = string
    instance    = string
    suffix      = string
    repository  = string
  })
}

variable "resource_group_name" {
  type = string
}

variable "entraid_admins" {
  type = list(object({
    principal_name = string
    principal_type = string
    principal_id   = string
  }))

  default = []
}

variable "location" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "key_vault_id" {
  type = string
}

variable "is_prod_like" {
  type = bool
}

variable "subnet_id" {
  type = string
}

variable "dns_zone" {
  type = string
}

variable "postgres_version" {
  default = "16"
  type    = string
}

variable "storage_mb" {
  default = 32768
  type    = number
  validation {
    condition     = contains([32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4193280, 4194304, 8388608, 16777216, 33553408], var.storage_mb)
    error_message = "possible values for 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4193280, 4194304, 8388608, 16777216, 33553408"
  }
}

