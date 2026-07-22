# Feature flags for the Access Management frontend BFF
# (https://github.com/Altinn/altinn-access-management-frontend).
# The frontend reads these from the hub App Configuration store using the
# "<env>-access-management-ui" label, keeping them separate from the backend
# flags in main.tf. Defined here until the frontend moves into this repo.
module "frontend_appsettings" {
  source     = "../../../../infra/modules/appsettings"
  hub_suffix = local.hub_suffix

  feature_flags = [
    {
      name        = "AccessManagementUI.DisplayPopularSingleRightsServices"
      description = "Whether or not to only display popular SingleRights services."
      label       = "${lower(var.environment)}-access-management-ui"
      default     = false
    },
    {
      name        = "AccessManagementUI.UseNewSingleRightsClientDelegation"
      description = "Whether or not to use the new version of client delegation for single rights services."
      label       = "${lower(var.environment)}-access-management-ui"
      default     = false
    },
    {
      name        = "AccessManagementUI.ShowHandledRequests"
      description = "Whether or not to show handled sent and received access package and resource requests."
      label       = "${lower(var.environment)}-access-management-ui"
      default     = false
    },
    {
      name        = "AccessManagementUI.ShowIdPortenAuthorizations"
      description = "Whether or not to include ID-porten authorizations in active consents list."
      label       = "${lower(var.environment)}-access-management-ui"
      default     = false
    },
  ]

  providers = {
    azurerm.hub = azurerm.hub
  }
}
