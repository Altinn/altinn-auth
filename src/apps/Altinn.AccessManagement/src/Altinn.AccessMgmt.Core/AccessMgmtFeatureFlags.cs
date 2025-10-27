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
    /// Specifies if the AuthorizedParties service should use entity framework services for lookup of accesspackages.
    /// </summary>
    public const string AuthorizedPartiesEntityFrameworkEnabled = $"AccessMgmt.Core.Services.AuthorizedParties.EfEnabled";
}
