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

variable "tenant_id" {
  type        = string
  description = "The Tenant ID represents the unique identifier of the Azure Active Directory (AAD) tenant where the resources will be deployed."
  default     = "cd0026d8-283b-4a55-9bfa-d0ef4a8ba21c"
}

variable "location" {
  type        = string
  description = " Specifies the Azure region where the resources will be provisioned. The location defines the physical datacenter where your resources will reside."
  default     = "norwayeast"
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

variable "ipv4_cidr" {
  type        = string
  description = "The Classless Inter-Domain Routing (CIDR) notation for the virtual network, defining the range of IP addresses that will be used in the network."
  default     = "10.202.0.0/20"
}

variable "ipv6_cidr" {
  type        = string
  description = "The Classless Inter-Domain Routing (CIDR) notation for the virtual network, defining the range of IP addresses that will be used in the network."
  default     = "fd39:ac82:d781::/48"
}

variable "api_domain" {
  type        = string
  description = "The domain name to be used in api host configurations"

  validation {
    condition     = endswith(var.api_domain, "altinn.no") || endswith(var.api_domain, "altinn.cloud")
    error_message = "The domain must end with '*.altinn.no' or '*.altinn.cloud'."
  }
}

variable "frontend_domain" {
  type        = string
  description = "The domain name to be used in frontend host configurations"

  validation {
    condition     = endswith(var.frontend_domain, "altinn.no") || endswith(var.frontend_domain, "altinn.cloud")
    error_message = "The domain must end with '*.altinn.no' or '*.altinn.cloud'."
  }
}

variable "cert_keyvault_name" {
  type        = string
  description = "The name of the Azure Key Vault where SSL/TLS certificates are stored"
}

variable "cert_resource_group_name" {
  type        = string
  description = "The name of the resource group that contains the Key Vault certificates"
}

variable "cert_user_assigned_identity_name" {
  type        = string
  description = "The name of the user-assigned managed identity that will be used for authenticating the cert Key Vault."
}

variable "services" {
  type = list(object(
    {
      domain   = string # Should be "api" or "frontend"
      path     = string # The path that will be exposed in the application gateway
      hostname = string # The backend pool associated with the path
    }
  ))

  default = [
    {
      domain   = "api" # Must be present
      path     = "accesspackages"
      domain   = "api"
      path     = "/accesspackages"
      hostname = "accesspackages"
    },
    {
      domain   = "frontend" # Must be present
      domain   = "frontend"
      path     = "/"
      hostname = "index"
    },
    {
      domain   = "api"
      path     = "/bootstrapper"
      hostname = "bootstrapper"
    }
  ]

  description = <<EOF
A list of backend API configurations for the application gateway. Each configuration includes:

- domain: Specifies the type of backend. It can only be "api" or "frontend".
- path: Represents the path exposed in the application gateway. Requests : "/{path}/*" gets routed to given hostname
- hostname: This refers to the backend pool corresponding to the path.

Routing logic:
- If domain is "frontend" and path is e.g. "accessmanagement", requests prefixed with "accessmanagement" are routed to:
  - accessmanagement.auth.{environment}.altinn.{no|cloud}.
  
- If domain is "api", requests will be routed to:
  - accessmanagement.api.auth.{environment}.altinn.{no|cloud}.
EOF

  validation {
    condition = alltrue([
      for item in var.services : contains(["api", "frontend"], item.domain)
    ])
    error_message = "Each backend entry's domain must be 'api' or 'frontend'."
  }
}
