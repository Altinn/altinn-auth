variable "subnet_id" {
  type = string
}

variable "hub_subscription_id" {
  type = string
}

variable "hub_suffix" {
  type = string
}

variable "suffix" {
  type = string
}

variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "key_vault_roles" {
  type = list(object(
    {
      operation_id         = string
      role_definition_name = string
      principal_id         = string
    }
  ))
  default = []
}
