variable "organization" {
  type = string
}

variable "product_name" {
  type = string
}

variable "instance" {
  type = string
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

variable "client_certs" {
  type = list(string)
}

variable "maintainers_principal_ids" {
  type = list(string)
}

variable "spoke_principal_ids" {
  type = list(string)
}

variable "developer_prod_principal_ids" {
  type = list(string)
}

variable "developer_dev_principal_ids" {
  type = list(string)
}
