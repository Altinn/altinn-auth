namespace Altinn.AccessManagement;

/// <summary>
/// Access Management Feature Flags.
/// </summary>
internal static class AccessManagementFeatureFlags
{
    /// <summary>
    /// Specifies if the register data should streamed from register service to access management database
    /// </summary>
    public const string HostedServicesRegisterSync = $"AccessManagement.HostedServices.RegisterSync";

        /// <summary>
    /// Specifies if the register data should streamed from register service to access management database
    /// </summary>
    public const string HostedServicesResourceRegistrySync = $"AccessManagement.HostedServices.ResourceRegistrySync";

    /// <summary>
    /// Specifies if migration should be done.
    /// </summary>
    public const string MigrationDb = $"AccessManagement.MigrationDb";
}
