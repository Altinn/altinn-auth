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
    /// Specifies if the resource registry data should be streamed from resource registry service to access management database
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
    public const string OutboxRequestPendingNotify = $"AccessMgmt.Core.Outbox.RequestPendingNotify";

    /// <summary>
    /// Specifies if notifications for approved requests are enabled.
    /// </summary>
    public const string OutboxRequestReviewedNotify = $"AccessMgmt.Core.Outbox.RequestReviewedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if rightholder assignemnt is added.
    /// </summary>
    public const string OutboxRightholderAddedNotify = $"AccessMgmt.Core.Outbox.RightholderAddedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if rightholder assignemnt is removed.
    /// </summary>
    public const string OutboxRightholderRemovedNotify = $"AccessMgmt.Core.Outbox.RightholderRemovedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if package is added.
    /// </summary>
    public const string OutboxAccessAddedNotify = $"AccessMgmt.Core.Outbox.AccessAddedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if package is removed.
    /// </summary>
    public const string OutboxAccessRemovedNotify = $"AccessMgmt.Core.Outbox.AccessRemovedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if aggent is added.
    /// </summary>
    public const string OutboxAgentAddedNotify = $"AccessMgmt.Core.Outbox.AgentAddedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if agent is removed.
    /// </summary>
    public const string OutboxAgentRemovedNotify = $"AccessMgmt.Core.Outbox.AgentRemovedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if client is added.
    /// </summary>
    public const string OutboxClientAddedNotify = $"AccessMgmt.Core.Outbox.ClientAddedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if client is removed.
    /// </summary>
    public const string OutboxClientRemovedNotify = $"AccessMgmt.Core.Outbox.ClientRemovedNotify";

    #endregion

    /// <summary>
    /// Enables the Maskinporten admin API endpoints (consumers and suppliers) in the enduser API.
    /// </summary>
    public const string EnableEnduserMaskinportenAdminApi = "AccessManagement.Enduser.MaskinportenAdminApi";

    /// <summary>
    /// Enables request assignment resource endpoints in enduser and serviceowner APIs.
    /// </summary>
    public const string EnableRequestAssignmentResource = "AccessMgmt.Controller.RequestAssignment.Resource";

    /// <summary>
    /// Enables to revoke Altinn 2 role assignments in enduser and serviceowner APIs.
    /// </summary>
    public const string Altinn2RoleRevoke = "AccessMgmt.Controller.Connection.RevokeRole";

    /// <summary>
    /// Enables request assignment package endpoints in enduser and serviceowner APIs.
    /// </summary>
    public const string EnableRequestAssignmentPackage = "AccessMgmt.Controller.RequestAssignment.Package";

    /// <summary>
    /// Specifies if AuthorizedParty should still perform SBL Bridge lookup of AuthorizedParties from Altinn 2.
    /// </summary>
    public const string AuthorizedPartiesIncludeAltinn2 = "AccessManagement.AuthorizedParties.IncludeAltinn2";

    /// <summary>
    /// Specifies if AuthorizedParty should use the new implementation based on lookup of all connection info (roles, packages, resources and instances) through the ConnectionQuery.
    /// </summary>
    public const string AuthorizedPartiesUsingConnectionQueryOnly = "AccessManagement.AuthorizedParties.UsingConnectionQueryOnly";
}
