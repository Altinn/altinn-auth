organization                    = "altinn"
product_name                    = "auth"
instance                        = "001"
single_stack_ipv4_address_space = "10.202.0.0/20"
dual_stack_ipv4_address_space   = "10.202.16.0/20"
dual_stack_ipv6_address_space   = "fd0a:7204:c37f::/56"
client_certs = [
  "remilovoll",
  "jonkjetil",
  "mariusthuen",
  "andreasisnes",
  "havardandersen",
  "aleksanderh",
  "runetlarsen",
  "simenr",
  "dhanalakshimi",
  "snehas",
  "vegardn",
  "ivart",
  "marting",
  "henningnormann",
  "rogerkjaernsrod",
  "sondrebraten",
  "wojciech",
  "github",
]
hub_principal_ids = [
  "a9585a64-20f0-4d18-aba6-9930f92b809c", # App: GitHub: altinn/altinn-auth - Prod
]
spoke_principal_ids = [
  "6eaed23e-df7f-4708-9c8e-a7f34deeadb4", # App: GitHub: altinn/altinn-auth - Dev
]
maintainers_principal_ids = [
  "93bed750-6ca4-47ae-ac43-b45fff4930f6", # Group: Altinn Product Authorization: Admins Dev
  "3863fbc0-a24b-42bf-af3d-f45111814457", # Group: Altinn Product Authorization: Admins Prod
  "a9585a64-20f0-4d18-aba6-9930f92b809c", # App: GitHub: altinn/altinn-auth - Prod
  "48587eaa-8f33-43ed-a0c3-108c3681e84b", # User: Nilsen, Andreas Isnes (ai-prod) -- NOTE: Temporarily
  "be1a510a-db1e-473c-a73a-558cdb68e353", # User: Nilsen, Andreas Isnes (ai-dev)  -- NOTE: Temporarily
]
developer_dev_principal_ids = [
  "6d54df21-3547-41a2-8d0d-529fad054807", # Group: Altinn Product Authorization: Developers Dev
]
developer_prod_principal_ids = [
  "c410f062-def4-44f5-9a45-b23ddcdd57c3", # Group: Altinn Product Authorization: Developers Prod
]
vpn_owners_principal_ids = [
  "6eaed23e-df7f-4708-9c8e-a7f34deeadb4", # App: GitHub: altinn/altinn-auth - Dev
  "a9585a64-20f0-4d18-aba6-9930f92b809c", # App: GitHub: altinn/altinn-auth - Prod
]
cd_principal_ids = [
  "6eaed23e-df7f-4708-9c8e-a7f34deeadb4", # App: GitHub: altinn/altinn-auth - Dev
  "a9585a64-20f0-4d18-aba6-9930f92b809c", # App: GitHub: altinn/altinn-auth - Prod
]
vpn_routes = {
  AuthorizationInfrastructure = [
    "10.202.0.0/16",
  ],
  AccesssManagementDb = [
    "20.100.174.95/32",  #at22
    "20.100.202.252/32", #at23
    "20.251.13.195/32",  #at24
    "51.13.65.140/32",   #yt01
    "20.251.56.54/32",   #tt02
  ],
  AuditlogsDb = [
    "20.100.186.204/32", #at22
  ],
  AuthenticationDb = [
    "51.13.60.10/32", # at22
  ],

  # Use  "name": "ServiceBus.NorwayEast"
  # https://www.microsoft.com/en-us/download/details.aspx?id=56519"
  "ServiceBusNoEast" = [
    "51.13.0.128/26",
    "51.120.76.34/32",
    "51.120.83.200/32",
    "51.120.98.16/29",
    "51.120.106.128/29",
    "51.120.109.208/28",
    "51.120.210.128/29",
    "51.120.213.48/28",
    "51.120.237.64/26",
    # "2603:1020:e04:1::220/123",
    # "2603:1020:e04:3::500/120",
    # "2603:1020:e04:402::170/125",
    # "2603:1020:e04:802::150/125",
    # "2603:1020:e04:c02::150/125"
  ]
}

feature_flags = {
  "access-management-ui" = [
    {
      name        = "AccessManagementUI.EnableActivityLog"
      description = "Whether or not to show menuitems and tabs for Activity Log."
    },
    {
      name        = "AccessManagementUI.DisplayPopularSingleRightsServices"
      description = "Whether or not to only display popular SingleRights services."
    },
    {
      name        = "AccessManagementUI.UseNewSingleRightsClientDelegation"
      description = "Whether or not to use the new version of client delegation for single rights services."
    },
    {
      name        = "AccessManagementUI.ShowHandledRequests"
      description = "Whether or not to show handled sent and received access package and resource requests."
    },
    {
      name        = "AccessManagementUI.ShowIdPortenAuthorizations"
      description = "Whether or not to include ID-porten authorizations in active consents list."
    },
    {
      name        = "AccessManagementUI.EnableSkyra"
      description = "Whether or not to load the Skyra survey SDK and ask for cookie consent."
    },
    {
      name        = "AccessManagementUI.EnableSingleRightsTabInPoaOverview"
      description = "Whether or not to show single rights tab in powers of attorney overview."
    },
  ],

  "access-management" = [
    {
      name        = "AccessMgmt.Core.Outbox.RequestPendingNotify"
      description = "Specifies if notifications for pending access requests are enabled."
    },
    {
      name        = "AccessMgmt.Core.Outbox.RequestReviewedNotify"
      description = "Specifies if notifications for reviewed access requests are enabled."
    },
    {
      name        = "AccessMgmt.Core.Outbox.RightholderAddedNotify"
      description = "Specifies if notifications should be sent when a rightholder assignment is added."
    },
    {
      name        = "AccessMgmt.Core.Outbox.RightholderRemovedNotify"
      description = "Specifies if notifications should be sent when a rightholder assignment is removed."
    },
    {
      name        = "AccessMgmt.Core.Outbox.AccessAddedNotify"
      description = "Specifies if notifications should be sent when access to a resource or package is granted."
    },
    {
      name        = "AccessMgmt.Core.Outbox.AccessRemovedNotify"
      description = "Specifies if notifications should be sent when access to a resource or package is revoked."
    },
    {
      name        = "AccessMgmt.Core.Outbox.AgentAddedNotify"
      description = "Specifies if notifications should be sent when an agent is added."
    },
    {
      name        = "AccessMgmt.Core.Outbox.AgentRemovedNotify"
      description = "Specifies if notifications should be sent when an agent is removed."
    },
    {
      name        = "AccessMgmt.Core.Outbox.ClientAddedNotify"
      description = "Specifies if notifications should be sent when an agent is added for a client."
    },
    {
      name        = "AccessMgmt.Core.Outbox.ClientRemovedNotify"
      description = "Specifies if notifications should be sent when an agent is removed from a client."
    },
    {
      name        = "AccessMgmt.Core.Outbox.InstanceAddedNotify"
      description = "Specifies if notifications should be sent when an instance is added."
    },
    {
      name        = "AccessMgmt.Core.Outbox.InstanceRemovedNotify"
      description = "Specifies if notifications should be sent when an instance is removed."
    },
    # Outbox
    {
      name        = "AccessMgmt.Core.HostedServices.Outbox.Handler"
      description = "Specifies if the outbox handler should be enabled."
    },
    {
      name        = "AccessMgmt.Core.HostedServices.Outbox.Reaper"
      description = "Specifies if the outbox reaper should be enabled."
    },
    {
      name        = "AccessMgmt.Core.HostedServices.RegisterSync"
      description = "(EF) Specifies if the resource register data should streamed from resource register service to access management database."
    },
    {
      name        = "AccessMgmt.Core.HostedServices.RegisterSync.Import"
      description = "(EF) Specifies if the register should do try to do the initial ingest of register data on init."
    },
    {
      name        = "AccessMgmt.Core.HostedServices.ResourceRegistrySync"
      description = "(EF) Specifies if the register data should streamed from register service to access management database."
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AllAltinnRoleSync"
      description = "Specifies if the Altinn II roles should be streamed from SBLBridge service to access management database"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AltinnClientRoleSync"
      description = "Specifies if the Altinn II client roles should streamed from SBLBridge service to access management database"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AltinnAdminRoleSync"
      description = "Specifies if the Altinn II admin roles should streamed from SBLBridge service to access management database"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AltinnBankruptcyEstateRoleSync"
      description = "Specifies if the Altinn II bankruptcy estate roles should streamed from SBLBridge service to access management database"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.AltinnPrivateTaxAffairRoleSync"
      description = "Specifies if the Altinn II private tax affair roles should be streamed from SBLBridge service to access management database as assignments with skatteforhold privatperson packages"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.SingleAppRightsSync"
      description = "Specifies if the single app right delegation should be read into the new database and connected to an assignment"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.SingleResourceRightsSync"
      description = "Specifies if the single resource right delegation should be read into the new database and connected to an assignment"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.SingleInstanceRightsSync"
      description = "Specifies if the single instance right delegation should be read into the new database and connected to an assignment"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.SingleAppRightsSync.FromErrorQueue"
      description = "Specifies if the single app right delegation should be read into the new database and connected to an assignment"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.SingleResourceRightsSync.FromErrorQueue"
      description = "Specifies if the single resource right delegation should be read into the new database and connected to an assignment"
    },
    {
      name        = "AccessMgmt.Core.HostedServices.SingleInstanceRightsSync.FromErrorQueue"
      description = "Specifies if the single instance right delegation should be read into the new database and connected to an assignment"
    },
    {
      name        = "AccessManagement.HostedServices.ResourceRegistrySync"
      description = "Specifies if the resource register data should streamed from resource register service to access management database."
    },
    {
      name        = "AccessManagement.HostedServices.RegisterSync"
      description = "Specifies if the register data should streamed from register service to access management database."
    },
    {
      name        = "AccessManagement.MigrationDb"
      description = "Specifies if database should be migrated using custom framework."
    },
    {
      name        = "AccessManagement.MigrationDbEf"
      description = "Specifies if database should be migrating using Entity Framework."
    },
    {
      name        = "AccessManagement.Enduser.Connections"
      description = "Specifies if feature access connections are enabled for endusers."
    },
    {
      name        = "AccessManagement.Internal.Connections"
      description = "Specifies if feature access connections are enabled for internal usage."
    },
    {
      name        = "AccessManagement.InstanceDelegation.EF"
      description = "Specifies if singlerights instaince delegation should use ef."
    },
    {
      name        = "AccessManagement.ResourceDelegation.EF"
      description = "Specifies if singlerights resource delegation should use ef."
    },
    {
      name        = "AccessMgmt.Controller.RequestAssignment.Resource"
      description = "Enables request assignment resource endpoints in enduser and serviceowner APIs."
    },
    {
      name        = "AccessMgmt.Controller.Connection.RevokeRole"
      description = "Enables to revoke Altinn 2 role assignments in enduser and serviceowner APIs."
    },
    {
      name        = "AccessMgmt.Controller.RequestAssignment.Package"
      description = "Enables request assignment package endpoints in enduser and serviceowner APIs."
    },
    {
      name        = "AccessMgmt.Controller.RequestAssignment.SystemUserRequests"
      description = "Enables system users (from-party) to send access requests to organizations. Trial feature; endpoint access is granted by the altinn:accessmanagement/systemuser:requests.write Maskinporten scope."
    },
    {
      name        = "AccessMgmt.Controller.ServiceOwner.ResourceDelegation"
      description = "Enables the resource delegation endpoints in the serviceowner connections API."
    },
    {
      name        = "AccessManagement.Enduser.MaskinportenAdminApi"
      description = "Enables the Maskinporten admin API endpoints (consumers and suppliers) in the enduser API."
    },
    {
      name        = "AccessManagement.AuthorizedParties.IncludeAltinn2"
      description = "Specifies if AuthorizedParty should still perform SBL Bridge lookup of AuthorizedParties from Altinn 2."
    },
    {
      name        = "AccessManagement.AuthorizedParties.UsingConnectionQueryOnly"
      description = "Specifies if AuthorizedParty should use the new implementation based on lookup of all connection info (roles, packages, resources and instances) through the ConnectionQuery."
    },
    {
      name        = "AccessManagement.Altinn2CacheInvalidation.Disable"
      description = "Setting this flag to true disables SblBridge calls for invalidating cache in Altinn 2."
    },
    {
      name        = "AccessManagement.Pip.IncludeClientDelegatedResources"
      description = "Specifies if v2 client-delegated resources should be included in PIP GetAllDelegationChanges response."
    },
    {
      name        = "AccessManagement.ConnectionQuery.IncludeClientDelegationResources"
      description = "Specifies if client-delegated resources should be included in ConnectionQuery and AuthorizedParties response."
    },
    {
      name        = "AccessManagement.Subunit.AdosInheritance"
      description = "Specifies if ADOS administrative units should be treated as subunits that inherit mainunit access (equal to BEDR/AAFY) in ConnectionQuery and AuthorizedParties. Reversible; defaults to false."
    }
  ]
}

