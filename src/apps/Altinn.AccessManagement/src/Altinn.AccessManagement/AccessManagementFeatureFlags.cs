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
    /// Specifies if the altinn roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAllAltinnRoleSync = $"AccessManagement.HostedServices.AllAltinnRoleSync";

    /// <summary>
    /// Specifies if the altinn client roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnClientRoleSync = $"AccessManagement.HostedServices.AltinnClientRoleSync";

    /// <summary>
    /// Specifies if the altinn admin roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnAdminRoleSync = $"AccessManagement.HostedServices.AltinnAdminRoleSync";

    /// <summary>
    /// Specifies if the altinn bancruptcy estate roles data should streamed from sblbridge service to access management database
    /// </summary>
    public const string HostedServicesAltinnBancruptcyEstateRoleSync = $"AccessManagement.HostedServices.AltinnBancruptcyEstateRoleSync";

    /// <summary>
    /// Specifies if migration should be done.
    /// </summary>
    public const string MigrationDb = $"AccessManagement.MigrationDb";

    /// <summary>
    /// Specifies if entity framework should manage migration.
    /// </summary>
    public const string MigrationDbEf = $"AccessManagement.MigrationDbEf";

    /// <summary>
    /// Specifies if migration should be done and include basic data for testing.
    /// </summary>
    public const string MigrationDbWithBasicData = $"AccessManagement.MigrationDbWithBasicData";
}
