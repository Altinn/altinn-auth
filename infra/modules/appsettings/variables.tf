variable "hub_suffix" {
  type = string
}

variable "feature_flags" {
  type = list(object(
    {
      default     = optional(bool, false)
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

variable "labels" {
  type = map(object({
    values = optional(
      map(object({
        value        = string
        content_type = optional(string)
      }))
    , {})
    vault_references = optional(
      map(object({
        vault_key_reference = string
      }))
    , {})
    feature_flags = optional(
      map(object({
        name        = optional(string)
        description = optional(string)
      }))
    , {})
  }))

  default = {}
}
