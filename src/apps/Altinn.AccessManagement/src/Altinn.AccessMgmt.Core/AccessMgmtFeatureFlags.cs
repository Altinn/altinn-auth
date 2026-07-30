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

    /// <summary>
    /// Specifies if notifications should be sent if instance is added.
    /// </summary>
    public const string OutboxInstanceAddedNotify = $"AccessMgmt.Core.Outbox.InstanceAddedNotify";

    /// <summary>
    /// Specifies if notifications should be sent if instance is removed.
    /// </summary>
    public const string OutboxInstanceRemovedNotify = $"AccessMgmt.Core.Outbox.InstanceRemovedNotify";

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
    /// Enables request assignment package endpoints in enduser and serviceowner APIs.
    /// </summary>
    public const string EnableRequestAssignmentPackage = "AccessMgmt.Controller.RequestAssignment.Package";

    /// <summary>
    /// Represents the configuration key used to disable cache invalidation for Altinn 2 cache.
    /// </summary>
    /// <remarks>Set this key in the application's configuration to prevent automatic invalidation of the
    /// Altinn 2 cache. To prevent errors when the endpoint no longer responds after Altinn 2 is decommissioned, 
    /// this flag should be enabled before the decommissioning date and remain enabled until the endpoint is removed from the codebase.
    /// </remarks>
    public const string DisableAltinn2CacheInvalidation = "AccessManagement.Altinn2CacheInvalidation.Disable";

    /// <summary>
    /// Feature flag for including v2 client-delegated resources in PIP GetAllDelegationChanges response.
    /// </summary>
    public const string IncludeClientDelegatedResourcesInPip = "AccessManagement.Pip.IncludeClientDelegatedResources";
}
