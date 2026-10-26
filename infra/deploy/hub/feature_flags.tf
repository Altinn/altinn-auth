locals {
  access_management_feature_flags = {
    "AccessManagement.Enduser.ConnectionsActivityLogApi"                     = { description = "Activity log endpoints under enduser/connections." },
    "AccessManagement.Enduser.RequestActivityLogApi"                         = { description = "Activity log endpoints under enduser/request." },
    "AccessManagement.Enduser.MaskinportenActivityLogApi"                    = { description = "Activity log endpoints under enduser/maskinporten." },
    "AccessMgmt.Core.Outbox.RequestPendingNotify"                            = { description = "Specifies if notifications for pending access requests are enabled." },
    "AccessMgmt.Core.Outbox.RequestReviewedNotify"                           = { description = "Specifies if notifications for reviewed access requests are enabled." },
    "AccessMgmt.Core.Outbox.RightholderAddedNotify"                          = { description = "Specifies if notifications should be sent when a rightholder assignment is added." },
    "AccessMgmt.Core.Outbox.RightholderRemovedNotify"                        = { description = "Specifies if notifications should be sent when a rightholder assignment is removed." },
    "AccessMgmt.Core.Outbox.AccessAddedNotify"                               = { description = "Specifies if notifications should be sent when access to a resource or package is granted." },
    "AccessMgmt.Core.Outbox.AccessRemovedNotify"                             = { description = "Specifies if notifications should be sent when access to a resource or package is revoked." },
    "AccessMgmt.Core.Outbox.AgentAddedNotify"                                = { description = "Specifies if notifications should be sent when an agent is added." },
    "AccessMgmt.Core.Outbox.AgentRemovedNotify"                              = { description = "Specifies if notifications should be sent when an agent is removed." },
    "AccessMgmt.Core.Outbox.ClientAddedNotify"                               = { description = "Specifies if notifications should be sent when an agent is added for a client." },
    "AccessMgmt.Core.Outbox.ClientRemovedNotify"                             = { description = "Specifies if notifications should be sent when an agent is removed from a client." },
    "AccessMgmt.Core.Outbox.InstanceAddedNotify"                             = { description = "Specifies if notifications should be sent when an instance is added." },
    "AccessMgmt.Core.Outbox.InstanceRemovedNotify"                           = { description = "Specifies if notifications should be sent when an instance is removed." },
    "AccessMgmt.Core.HostedServices.Outbox.Handler"                          = { description = "Specifies if the outbox handler should be enabled." },
    "AccessMgmt.Core.HostedServices.Outbox.Reaper"                           = { description = "Specifies if the outbox reaper should be enabled." },
    "AccessMgmt.Core.HostedServices.RegisterSync"                            = { description = "(EF) Specifies if the resource register data should streamed from resource register service to access management database." },
    "AccessMgmt.Core.HostedServices.RegisterSync.Import"                     = { description = "(EF) Specifies if the register should do try to do the initial ingest of register data on init." },
    "AccessMgmt.Core.HostedServices.ResourceRegistrySync"                    = { description = "(EF) Specifies if the register data should streamed from register service to access management database." },
    "AccessMgmt.Core.HostedServices.AllAltinnRoleSync"                       = { description = "Specifies if the Altinn II roles should be streamed from SBLBridge service to access management database" },
    "AccessMgmt.Core.HostedServices.AltinnClientRoleSync"                    = { description = "Specifies if the Altinn II client roles should streamed from SBLBridge service to access management database" },
    "AccessMgmt.Core.HostedServices.AltinnAdminRoleSync"                     = { description = "Specifies if the Altinn II admin roles should streamed from SBLBridge service to access management database" },
    "AccessMgmt.Core.HostedServices.AltinnBankruptcyEstateRoleSync"          = { description = "Specifies if the Altinn II bankruptcy estate roles should streamed from SBLBridge service to access management database" },
    "AccessMgmt.Core.HostedServices.AltinnPrivateTaxAffairRoleSync"          = { description = "Specifies if the Altinn II private tax affair roles should be streamed from SBLBridge service to access management database as assignments with skatteforhold privatperson packages" },
    "AccessMgmt.Core.HostedServices.SingleAppRightsSync"                     = { description = "Specifies if the single app right delegation should be read into the new database and connected to an assignment" },
    "AccessMgmt.Core.HostedServices.SingleResourceRightsSync"                = { description = "Specifies if the single resource right delegation should be read into the new database and connected to an assignment" },
    "AccessMgmt.Core.HostedServices.SingleInstanceRightsSync"                = { description = "Specifies if the single instance right delegation should be read into the new database and connected to an assignment" },
    "AccessMgmt.Core.HostedServices.SingleAppRightsSync.FromErrorQueue"      = { description = "Specifies if the single app right delegation should be read into the new database and connected to an assignment" },
    "AccessMgmt.Core.HostedServices.SingleResourceRightsSync.FromErrorQueue" = { description = "Specifies if the single resource right delegation should be read into the new database and connected to an assignment" },
    "AccessMgmt.Core.HostedServices.SingleInstanceRightsSync.FromErrorQueue" = { description = "Specifies if the single instance right delegation should be read into the new database and connected to an assignment" },
    "AccessManagement.HostedServices.ResourceRegistrySync"                   = { description = "Specifies if the resource register data should streamed from resource register service to access management database." },
    "AccessManagement.HostedServices.RegisterSync"                           = { description = "Specifies if the register data should streamed from register service to access management database." },
    "AccessManagement.MigrationDb"                                           = { description = "Specifies if database should be migrated using custom framework." },
    "AccessManagement.MigrationDbEf"                                         = { description = "Specifies if database should be migrating using Entity Framework." },
    "AccessManagement.Enduser.Connections"                                   = { description = "Specifies if feature access connections are enabled for endusers." },
    "AccessManagement.Internal.Connections"                                  = { description = "Specifies if feature access connections are enabled for internal usage." },
    "AccessManagement.InstanceDelegation.EF"                                 = { description = "Specifies if singlerights instaince delegation should use ef." },
    "AccessManagement.ResourceDelegation.EF"                                 = { description = "Specifies if singlerights resource delegation should use ef." },
    "AccessMgmt.Controller.RequestAssignment.Resource"                       = { description = "Enables request assignment resource endpoints in enduser and serviceowner APIs." },
    "AccessMgmt.Controller.Connection.RevokeRole"                            = { description = "Enables to revoke Altinn 2 role assignments in enduser and serviceowner APIs." },
    "AccessMgmt.Controller.RequestAssignment.Package"                        = { description = "Enables request assignment package endpoints in enduser and serviceowner APIs." },
    "AccessMgmt.Controller.RequestAssignment.SystemUserRequests"             = { description = "Enables system users (from-party) to send access requests to organizations. Trial feature; endpoint access is granted by the altinn:accessmanagement/systemuser:requests.write Maskinporten scope." },
    "AccessMgmt.Controller.ServiceOwner.ResourceDelegation"                  = { description = "Enables the resource delegation endpoints in the serviceowner connections API." },
    "AccessManagement.Enduser.MaskinportenAdminApi"                          = { description = "Enables the Maskinporten admin API endpoints (consumers and suppliers) in the enduser API." },
    "AccessManagement.AuthorizedParties.IncludeAltinn2"                      = { description = "Specifies if AuthorizedParty should still perform SBL Bridge lookup of AuthorizedParties from Altinn 2." },
    "AccessManagement.AuthorizedParties.UsingConnectionQueryOnly"            = { description = "Specifies if AuthorizedParty should use the new implementation based on lookup of all connection info (roles, packages, resources and instances) through the ConnectionQuery." },
    "AccessManagement.Altinn2CacheInvalidation.Disable"                      = { description = "Setting this flag to true disables SblBridge calls for invalidating cache in Altinn 2." },
    "AccessManagement.Pip.IncludeClientDelegatedResources"                   = { description = "Specifies if v2 client-delegated resources should be included in PIP GetAllDelegationChanges response." },
    "AccessManagement.ConnectionQuery.IncludeClientDelegationResources"      = { description = "Specifies if client-delegated resources should be included in ConnectionQuery and AuthorizedParties response." },
    "AccessManagement.Subunit.AdosInheritance"                               = { description = "Specifies if ADOS administrative units should be treated as subunits that inherit mainunit access (equal to BEDR/AAFY) in ConnectionQuery and AuthorizedParties. Reversible; defaults to false." },
    "AccessMgmt.Core.HostedServices.ResourceRegistrySync.ChangeFeed"         = { description = "Specifies if we should use the new change feed to process all resources before we start to process the permission or do as before only process the permission and update resources with changed permissions." },
  }

  access_management_feature_flags_ui = {
    "AccessManagement.AuthorizedParties.SubAndInactivePartiesFilters" = { description = "Specifies whether AuthorizedParties applies the includeSubParties and includeInactiveParties filters or retains the legacy behavior of treating both as true." },
    "AccessManagementUI.EnableActivityLog"                            = { description = "Whether or not to show menuitems and tabs for Activity Log." },
    "AccessManagementUI.DisplayPopularSingleRightsServices"           = { description = "Whether or not to only display popular SingleRights services." },
    "AccessManagementUI.UseNewSingleRightsClientDelegation"           = { description = "Whether or not to use the new version of client delegation for single rights services." },
    "AccessManagementUI.ShowHandledRequests"                          = { description = "Whether or not to show handled sent and received access package and resource requests." },
    "AccessManagementUI.ShowIdPortenAuthorizations"                   = { description = "Whether or not to include ID-porten authorizations in active consents list." },
    "AccessManagementUI.EnableSkyra"                                  = { description = "Whether or not to load the Skyra survey SDK and ask for cookie consent." },
    "AccessManagementUI.EnableSingleRightsTabInPoaOverview"           = { description = "Whether or not to show single rights tab in powers of attorney overview." },
  }
}

module "access_management_feature_flags" {
  source                 = "../../modules/feature_flag"
  app                    = "access-management"
  name                   = each.key
  description            = each.value.description
  configuration_store_id = azurerm_app_configuration.app_configuration.id

  providers = {
    azurerm.hub = azurerm
  }

  for_each = local.access_management_feature_flags
}

module "access_management_feature_flags_ui" {
  source                 = "../../modules/feature_flag"
  app                    = "access-management-ui"
  name                   = each.key
  description            = each.value.description
  configuration_store_id = azurerm_app_configuration.app_configuration.id

  providers = {
    azurerm.hub = azurerm
  }

  for_each = local.access_management_feature_flags_ui
}
