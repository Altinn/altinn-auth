namespace Altinn.AccessManagement;

/// <summary>
/// Access Management Feature Flags.
/// </summary>
internal static class AccessManagementFeatureFlags
{
    /// <summary>
    /// Specifies if the register data should streamed from register service to access management database
    /// </summary>
    public const string SyncRegister = $"AccessManagement.HostedServices.RegisterSync";

    /// <summary>
    /// Specifes if Authorized Parties should be enabled
    /// </summary>
    public const string ControllerAuthorizedParties = "AccessManagement.Controller.AuthorizedParties";
}
