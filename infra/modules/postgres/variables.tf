variable "suffix" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "private_dns_zone_id" {
  type = string
}

variable "subnet_id" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "tier" {
  type = ""
}

variable "compute_tier" {
  type = string
  validation {
    condition     = contains(["Burstable", "GeneralPurpose", "MemoryOptimized"], var.sku)
    error_message = "Possible values are Burstable, GeneralPurpose and MemoryOptimized"
  }

  default     = "Burstable" # Cheapest
  description = "Compute tier"
}

variable "compute_size" {
  type = string
}

variable "entraid_admins" {
  type = list(object({
    object_id      = string
    principal_name = string
    principal_type = string
  }))

  description = "Possible values for 'principal_type' are Group, ServicePrincipal and User"
}

variable "postgres_version" {
  type    = string
  default = "16"
}

variable "storage_mb" {
  default = 32768
  type    = number
  validation {
    condition     = contains([32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4193280, 4194304, 8388608, 16777216, 33553408], var.storage_mb)
    error_message = "possible values for 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4193280, 4194304, 8388608, 16777216, 33553408"
  }
}
