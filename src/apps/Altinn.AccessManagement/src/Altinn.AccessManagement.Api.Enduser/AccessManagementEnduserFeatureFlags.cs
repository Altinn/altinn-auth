namespace Altinn.AccessManagement.Api.Enduser;

/// <summary>
/// Defines feature flags for the Access Management Enduser module.
/// </summary>
internal static class AccessManagementEnduserFeatureFlags
{
    /// <summary>
    /// Feature flag for controlling access to parties in the Access Management Enduser module.
    /// </summary>
    public const string ControllerAccessParties = "AccessManagement.Enduser.AccessParties";

    /// <summary>
    /// Feature flag for controlling access to connections in the Access Management Enduser module.
    /// </summary>
    public const string ControllerConnections = "AccessManagement.Enduser.Connections";
}
