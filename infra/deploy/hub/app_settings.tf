locals {
  environments = ["at22", "at23", "at24", "yt01", "tt02", "prod"]

  feature_flags = flatten([
    for environment in local.environments : [
      for label, flags in var.feature_flags : [
        [for flag in flags :
          {
            label       = "${environment}-${label}"
            name        = flag.name
            description = flag.description
            default     = false
          }
        ]
      ]
    ]
  ])

  migrate_feature_flags = flatten([
    for environment in local.environments : [
      [
      ]
    ]
  ])
}

module "appsettings" {
  source     = "../../modules/appsettings"
  hub_suffix = local.suffix

  feature_flags = local.feature_flags

  providers = {
    azurerm.hub = azurerm
  }
}


# id=https://appconfaltinnauth001hub.azconfig.io/kv/.appconfig.featureflag%2Ftest?label=at22-access-management
import {
  for_each = toset(local.migrate_feature_flags)

  to = module.appsettings.azurerm_app_configuration_feature.configuration[
    "${each.value.name}_${each.value.label}"
  ]

  id = "https://appconfaltinnauth001hub.azconfig.io/kv/.appconfig.featureflag%2F${each.value.name}?label=${each.value.label}"
}
