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

variable "maintainers" {
  type = list(string)
}
