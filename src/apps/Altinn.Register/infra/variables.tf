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
  default = "register"
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

variable "deploy_app_principal_id" {
  type = string
}

variable "aks_federation" {
  type = list(object({
    issuer_url      = string
    namespace       = string
    service_account = string
  }))
}

variable "platform_workflow_principal_ids" {
  type    = list(string)
  default = []
}
