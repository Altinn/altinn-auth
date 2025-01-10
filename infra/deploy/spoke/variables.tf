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

variable "environment_group" {
  type    = string
  default = "at"
}

variable "ipv4_address_space" {
  type    = string
  default = "10.202.0.0/17"
}

# 10.202.128.0/17	

variable "ipv6_address_space" {
  type    = string
  default = "fd0a:7204:c37f::/51"
}

