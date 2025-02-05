variable "hub_suffix" {
  type = string
}

variable "feature_flags" {
  type = list(object(
    {
      name        = string
      label       = string
      description = string
  }))

  default = []
}

variable "key_value" {
  type = list(object(
    {
      key   = string
      value = string
      label = string
    }
  ))

  default = []
}

variable "key_vault_reference" {
  type = list(object(
    {
      key                 = string
      key_vault_secret_id = string
      label               = string
    }
  ))

  default = []
}
