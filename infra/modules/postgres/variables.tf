variable "prefix" {
  type    = string
  default = ""
}

variable "hub_suffix" {
  type = string
}

variable "suffix" {
  type    = string
  default = ""
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

variable "storage_tier" {
  type    = string
  default = "P10"
}

variable "configurations" {
  type    = map(string)
  default = {}
}

variable "compute_tier" {
  type = string
  validation {
    condition     = contains(["GeneralPurpose", "MemoryOptimized"], var.compute_tier)
    error_message = "Possible values are GeneralPurpose and MemoryOptimized"
  }

  default     = "GeneralPurpose" # Cheapest
  description = "Compute tier"
}

variable "compute_size" {
  type = string
}

variable "backup_retention_days" {
  type        = number
  default     = 14
  description = "(Optional) The backup retention days for the PostgreSQL Flexible Server. Possible values are between 7 and 35 days."
  validation {
    condition     = 7 <= var.backup_retention_days && var.backup_retention_days <= 35
    error_message = "must be between 7 and 35 days, are ${var.backup_retention_days}"
  }
}

variable "entraid_admins" {
  type = list(object({
    principal_id   = string
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
