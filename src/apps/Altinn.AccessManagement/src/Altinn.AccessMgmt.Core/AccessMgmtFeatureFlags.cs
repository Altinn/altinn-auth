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
    /// Specifies if the register data should streamed from register service to access management database
    /// </summary>
    public const string HostedServicesRegisterSync = $"AccessMgmt.Core.HostedServices.RegisterSync";

    /// <summary>
    /// Specifies if the register data should streamed from register service to access management database
    /// </summary>
    public const string HostedServicesRegisterSyncImport = $"AccessMgmt.Core.HostedServices.RegisterSync.Import";

    /// <summary>
    /// Specifies if the register data should streamed from register service to access management database
    /// </summary>
    public const string HostedServicesResourceRegistrySync = $"AccessMgmt.Core.HostedServices.ResourceRegistrySync";

    /// <summary>
    /// Specifies if the altinn roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAllAltinnRoleSync = $"AccessMgmt.Core.HostedServices.AllAltinnRoleSync";

    /// <summary>
    /// Specifies if the altinn client roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnClientRoleSync = $"AccessMgmt.Core.HostedServices.AltinnClientRoleSync";

    /// <summary>
    /// Specifies if the altinn admin roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnAdminRoleSync = $"AccessMgmt.Core.HostedServices.AltinnAdminRoleSync";

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
}
