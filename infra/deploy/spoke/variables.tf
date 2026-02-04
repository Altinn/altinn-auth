variable "organization" {
  type = string
}

variable "product_name" {
  type = string
}

variable "instance" {
  type = string
}

variable "environment" {
  type = string
}

variable "prod_like" {
  type = bool
}

variable "single_stack_ipv4_address_space" {
  type = string
}

variable "dual_stack_ipv4_address_space" {
  type = string
}

variable "dual_stack_ipv6_address_space" {
  type = string
}

variable "hub_subscription_id" {
  type    = string
  default = "01de49cb-48ef-4494-bc9d-b9e19a90bcd5"
}

variable "hub_principal_id" {
  type    = string
  default = "a9585a64-20f0-4d18-aba6-9930f92b809c"
}

variable "spoke_principal_ids" {
  type = list(string)
}

variable "firewall_private_ipv4" {
  type    = string
  default = "10.202.19.4"
}

variable "firewall_public_ipv4" {
  type    = string
  default = "51.120.88.253"
}

variable "service_bus_firewall" {
  type    = list(string)
  default = []
}

variable "appconfiguration" {
  type = object({
    platform_resource_registry_endpoint = string
    platform_register_endpoint          = string
    platform_sbl_bridge_endpoint        = string
    maskinporten_endpoint               = string
  })
}

variable "services" {
  type = map(object({
    protocol = optional(string, "https")
    host     = string
  }))
}

variable "logging" {
  type = object({
    min_level = map(string)
  })
}
