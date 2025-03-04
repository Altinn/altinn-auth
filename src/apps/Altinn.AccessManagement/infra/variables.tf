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
  default = "accessmgmt"
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

variable "pg_dns_hex" {
  type    = string
  default = ""
}

variable "db_admins_user_principal_ids" {
  type = list(object(
    {
      principal_id   = string
      principal_name = string
      principal_type = string
  }))

  default = [{
    principal_id   = "4241b5ee-326f-4359-bde7-ee1a99287d7f",
    principal_name = "ext-mthue-prod@ai-dev.no",
    principal_type = "User"
    },
    {
      principal_id   = "be1a510a-db1e-473c-a73a-558cdb68e353",
      principal_name = "ext-anils@ai-dev.no",
      principal_type = "User"
    },
    {
      principal_id   = "3ab09791-7fa4-49ec-95ae-cd84b51ab691",
      principal_name = "acn-joye-prod@ai-dev.no",
      principal_type = "User"
  }]
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
