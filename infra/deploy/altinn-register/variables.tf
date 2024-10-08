variable "environment" {
  type        = string
  description = <<EOT
Specifies the target environment where the infrastructure will be deployed. 
It supports specific environment values, including 'at21', 'at22', 'at23', 'at24', 'at25', 'yt01', 'tt02', and 'prod'. 
This variable is used to differentiate between various deployment environments, such as testing (at/yt), staging (tt02), or (prod).
  EOT
  validation {
    condition     = contains(["at21", "at22", "at23", "at24", "at25", "yt01", "tt02", "prod"], var.environment)
    error_message = "The environment must be one of the following: at21, at22, at23, at24, at25, yt01, tt02, prod."
  }
}

variable "instance" {
  type        = string
  description = "A string to represent the specific instance of the deployment, used for resource naming. Used distinguishing between different deployments of the same infrastructure."
  default     = "001"
}

variable "is_prod_like" {
  type        = bool
  description = "A boolean flag indicating whether the deployment is similar to a production environment. This can affect resource configurations and settings."
  default     = false
}

variable "location" {
  type        = string
  description = "Specifies the Azure region where the resources will be provisioned. The location defines the physical datacenter where your resources will reside."
  default     = "norwayeast"
}

variable "infrastructure_name" {
  type        = string
  description = "Name of auth/shared infrastructure deployment"
  default     = "auth"
}
