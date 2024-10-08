variable "infrastructure_name" {
  type    = string
  default = "auth"
}

variable "instance" {
  type    = string
  default = "001"
}

variable "location" {
  type        = string
  default     = "norwayeast"
  description = "Specifies the Azure region where the resources will be provisioned (e.g., 'norwayeast')."
}

variable "variables" {
  type    = map(string)
  default = {}
}

variable "user_assigned_identities" {
  type        = list(string)
  default     = []
  description = "List of principal IDs"
}

variable "environment" {
  type = string
}

variable "name" {
  type = string
}

variable "image" {
  type = string
}

variable "registry" {
  type    = string
  default = "ghcr.io"
}

variable "can_use_auth_service_bus" {
  type    = bool
  default = false
}

variable "can_use_auth_key_vault" {
  type    = bool
  default = false
}

variable "can_use_auth_app_configuration" {
  type    = bool
  default = false
}

variable "max_replicas" {
  type    = number
  default = 5
}

variable "allocated_memory" {
  type    = number
  default = 0.5
}

variable "alloacated_cpu" {
  type    = number
  default = 0.25
}

variable "app_configuration_variables" {
  type    = map(string)
  default = {}
}
