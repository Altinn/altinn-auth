namespace Altinn.AccessMgmt.Core;

/// <summary>
/// Feature flags for Access Management
/// </summary>
public static class AccessMgmtFeatureFlags
{
    /// <summary>
    /// Feature flag for enabling consent migration from Altinn 2 to Altinn 3. This flag controls whether the hosted service responsible for migrating consents is active.
    /// </summary>
    public const string HostedServicesConsentMigration = "AccessMgmt.Core.HostedServices.ConsentMigration";

    /// <summary>
    /// Specifies if the register data should be streamed from register service to access management database
    /// </summary>
    public const string HostedServicesRegisterSync = $"AccessMgmt.Core.HostedServices.RegisterSync";

    /// <summary>
    /// Specifies if the register data should be streamed from register service to access management database
    /// </summary>
    public const string HostedServicesRegisterSyncImport = $"AccessMgmt.Core.HostedServices.RegisterSync.Import";

    /// <summary>
    /// Specifies if the register data should be streamed from register service to access management database
    /// </summary>
    public const string HostedServicesResourceRegistrySync = $"AccessMgmt.Core.HostedServices.ResourceRegistrySync";

    /// <summary>
    /// Specifies if the altinn roles data should be streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAllAltinnRoleSync = $"AccessMgmt.Core.HostedServices.AllAltinnRoleSync";

    /// <summary>
    /// Specifies if the altinn client roles data should be streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnClientRoleSync = $"AccessMgmt.Core.HostedServices.AltinnClientRoleSync";

    /// <summary>
    /// Specifies if the altinn admin roles data should be streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnAdminRoleSync = $"AccessMgmt.Core.HostedServices.AltinnAdminRoleSync";

    /// <summary>
    /// Specifies if the altinn private tax affair roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesPrivateTaxAffairRoleSync = $"AccessMgmt.Core.HostedServices.AltinnPrivateTaxAffairRoleSync";

    /// <summary>
    /// Specifies if the altinn bankruptcyestate roles data should be streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnBankruptcyEstateRoleSync = $"AccessMgmt.Core.HostedServices.AltinnBankruptcyEstateRoleSync";

    /// <summary>
    /// Specifies if AuthorizedPartiesServiceEf should be used
    /// </summary>
    public const string AuthorizedPartiesEfEnabled = $"AccessMgmt.Core.Services.AuthorizedParties.EfEnabled";

    /// <summary>
    /// Specifies Client Delegation should be enabled in enduser API.
    /// </summary>
    public const string EnduserControllerClientDelegation = $"AccessMgmt.Enduser.Controller.ClientDelegation";

    /// <summary>
    /// Represents the resource name for the hosted service responsible for synchronizing single application rights.
    /// </summary>
    public const string HostedServicesSingleAppRightSync = $"AccessMgmt.Core.HostedServices.SingleAppRightsSync";

    /// <summary>
    /// Represents the resource name for the hosted service responsible for synchronizing single resourceregistry rights.
    /// </summary>
    public const string HostedServicesSingleResourceRightSync = $"AccessMgmt.Core.HostedServices.SingleResourceRightsSync";

    /// <summary>
    /// Represents the resource name for the hosted service responsible for synchronizing single instance rights.
    /// </summary>
    public const string HostedServicesSingleInstanceRightSync = $"AccessMgmt.Core.HostedServices.SingleInstanceRightsSync";

    /// <summary>
    /// Represents the resource name for the hosted service responsible for synchronizing single application rights.
    /// </summary>
    public const string HostedServicesSingleAppRightSyncFromErrorQueue = $"AccessMgmt.Core.HostedServices.SingleAppRightsSync.FromErrorQueue";

    /// <summary>
    /// Represents the resource name for the hosted service responsible for synchronizing single resourceregistry rights.
    /// </summary>
    public const string HostedServicesSingleResourceRightSyncFromErrorQueue = $"AccessMgmt.Core.HostedServices.SingleResourceRightsSync.FromErrorQueue";

    /// <summary>
    /// Represents the resource name for the hosted service responsible for synchronizing single instance rights.
    /// </summary>
    public const string HostedServicesSingleInstanceRightSyncFromErrorQueue = $"AccessMgmt.Core.HostedServices.SingleInstanceRightsSync.FromErrorQueue";

    /// <summary>
    /// Represents the handler for proessing pending outbox messages.
    /// </summary>
    public const string HostedServicesOutboxHandler = $"AccessMgmt.Core.HostedServices.Outbox.Handler";

    /// <summary>
    /// Represents the handler for outbox messages that couldn't be processed.
    /// </summary>
    public const string HostedServicesOutboxReaper = $"AccessMgmt.Core.HostedServices.Outbox.Reaper";

    #region Outbox

    /// <summary>
    /// Specifies if notifications for pending requests are enabled.
    /// </summary>
    public const string AccessMgmtCoreOutboxRequestNotifyPending = $"AccessMgmt.Core.Outbox.RequestNotifyPending";

    /// <summary>
    /// Specifies if notifications for approved requests are enabled.
    /// </summary>
    [Obsolete($"will be removed once {nameof(AccessMgmtCoreOutboxRequestNotifyReviewed)} is in production.")]
    public const string AccessMgmtCoreOutboxRequestNotifyApproved = $"AccessMgmt.Core.Outbox.RequestNotifyApproved";

    /// <summary>
    /// Specifies if notifications for approved requests are enabled.
    /// </summary>
    public const string AccessMgmtCoreOutboxRequestNotifyReviewed = $"AccessMgmt.Core.Outbox.RequestNotifyReviewed";

    /// <summary>
    /// Specifies if notifications should be sent if rightholder assignemnt is added.
    /// </summary>
    public const string AccessMgmtCoreOutboxRightholderNotifyAdded = $"AccessMgmt.Core.Outbox.RightholderNotifyAdded";

    /// <summary>
    /// Specifies if notifications should be sent if rightholder assignemnt is removed.
    /// </summary>
    public const string AccessMgmtCoreOutboxRightholderNotifyRemoved = $"AccessMgmt.Core.Outbox.RightholderNotifyRemoved";

    /// <summary>
    /// Specifies if notifications should be sent if package is added.
    /// </summary>
    public const string AccessMgmtCoreOutboxPackageNotifyAdded = $"AccessMgmt.Core.Outbox.PackageNotifyAdded";

    /// <summary>
    /// Specifies if notifications should be sent if package is removed.
    /// </summary>
    public const string AccessMgmtCoreOutboxPackageNotifyRemoved = $"AccessMgmt.Core.Outbox.PackageNotifyRemoved";

    /// <summary>
    /// Specifies if notifications should be sent if resource is added.
    /// </summary>
    public const string AccessMgmtCoreOutboxResourceNotifyAdded = $"AccessMgmt.Core.Outbox.ResourceNotifyAdded";

    /// <summary>
    /// Specifies if notifications should be sent if resource is removed.
    /// </summary>
    public const string AccessMgmtCoreOutboxResourceNotifyRemoved = $"AccessMgmt.Core.Outbox.ResourceNotifyRemoved";

    /// <summary>
    /// Specifies if notifications should be sent if aggent is added.
    /// </summary>
    public const string AccessMgmtCoreOutboxAgentNotifyAdded = $"AccessMgmt.Core.Outbox.AgentNotifyAdded";

    /// <summary>
    /// Specifies if notifications should be sent if agent is removed.
    /// </summary>
    public const string AccessMgmtCoreOutboxAgentNotifyRemoved = $"AccessMgmt.Core.Outbox.AgentNotifyRemoved";

    /// <summary>
    /// Specifies if notifications should be sent if client is added.
    /// </summary>
    public const string AccessMgmtCoreOutboxClientNotifyAdded = $"AccessMgmt.Core.Outbox.ClientNotifyAdded";

    /// <summary>
    /// Specifies if notifications should be sent if client is removed.
    /// </summary>
    public const string AccessMgmtCoreOutboxClientNotifyRemoved = $"AccessMgmt.Core.Outbox.ClientNotifyRemoved";

    #endregion

    /// <summary>
    /// Feature flag for Controller Enduser Connections
    /// </summary>
    public const string EnduserControllerConnections = "AccessManagement.Enduser.Connections";

    /// <summary>
    /// Enables request assignment resource endpoints in enduser and serviceowner APIs.
    /// </summary>
    public const string EnableRequestAssignmentResource = "AccessMgmt.Controller.RequestAssignment.Resource";

    /// <summary>
    /// Enables request assignment package endpoints in enduser and serviceowner APIs.
    /// </summary>
    public const string EnableRequestAssignmentPackage = "AccessMgmt.Controller.RequestAssignment.Package";

    /// <summary>
    /// Specifies if entity framework implementation of instance delegations should be used.
    /// </summary>
    public const string InstanceDbEf = $"AccessManagement.InstanceDelegation.EF";

    /// <summary>
    /// Specifies if entity framework implementation of resource delegations should be used.
    /// </summary>
    public const string ResourceDelegationEF = "AccessManagement.ResourceDelegation.EF";
}
