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
  default = "authorization"
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

variable "aks_federation" {
  type = list(object({
    issuer_url      = string
    namespace       = string
    service_account = string
  }))
}
