namespace Altinn.AccessManagement;

/// <summary>
/// Access Management Feature Flags.
/// </summary>
internal static class AccessManagementFeatureFlags
{
    /// <summary>
    /// Specifies if migration should be done and include basic data for testing.
    /// </summary>
    public const string MigrationDbWithBasicData = $"AccessManagement.MigrationDbWithBasicData";
}
