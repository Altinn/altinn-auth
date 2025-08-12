locals {
  compute_skus = {
    "D2" = {
      sku_name        = "GeneralPurpose_Standard_D2ads_v5"
      max_connections = 859
    }
    "D4" = {
      sku_name        = "GeneralPurpose_Standard_D4ads_v5"
      max_connections = 1718
    }
    "D8" = {
      sku_name        = "GeneralPurpose_Standard_D8ads_v5"
      max_connections = 3437
    }
  }
}
