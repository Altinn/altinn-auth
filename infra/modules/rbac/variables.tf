variable "hub_suffix" {
  type = string
}

variable "spoke_suffix" {
  type = string
}

variable "principal_id" {
  type = string
}

variable "use_app_configuration" {
  type    = bool
  default = false
}

variable "use_masstransit" {
  type    = bool
  default = false
}

variable "use_lease" {
  type    = bool
  default = false
}

variable "use_key_vault_secrets" {
  type    = list(string)
  default = [""]
}
