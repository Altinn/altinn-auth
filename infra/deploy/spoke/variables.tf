variable "organization" {
  type = string
}

variable "product_name" {
  type = string
}

variable "instance" {
  type = string
}

variable "environment" {
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

variable "hub_subscription_id" {
  type    = string
  default = "01de49cb-48ef-4494-bc9d-b9e19a90bcd5"
}

variable "forced_tunneling_ip" {
  type    = string
  default = "10.202.19.4"
}
