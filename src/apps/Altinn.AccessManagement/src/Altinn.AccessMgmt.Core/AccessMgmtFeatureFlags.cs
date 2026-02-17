namespace Altinn.AccessMgmt.Core;

public static class AccessMgmtFeatureFlags
{
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
}
