variable "organization" {
  type    = string
  default = "altinn"
}

variable "product_name" {
  type    = string
  default = "auth"
}

variable "instance" {
  type    = string
  default = "001"
}

variable "single_stack_ipv4_address_space" {
  type    = string
  default = "10.202.0.0/20"
}

variable "dual_stack_ipv4_address_space" {
  type    = string
  default = "10.202.16.0/20"
}

variable "dual_stack_ipv6_address_space" {
  type    = string
  default = "fd0a:7204:c37f::/51"
}

variable "client_certs" {
  type = list(string)
  default = [
    "andreasisnes"
  ]
}
