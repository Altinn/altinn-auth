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

variable "image" {
  type        = string
  description = "Image of the resource that should be deployed"
}

variable "infrastructure_name" {
  type    = string
  default = "auth"
}
