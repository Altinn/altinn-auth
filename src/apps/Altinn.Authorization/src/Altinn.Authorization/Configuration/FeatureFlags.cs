namespace Altinn.Platform.Authorization.Configuration
{
    /// <summary>
    /// Feature flags 
    /// </summary>
    public static class FeatureFlags
    {
        /// <summary>
        /// audit log flag
        /// </summary>
        public const string AuditLog = "AuditLog";

        /// <summary>
        /// Feature flag for whether parties API should use AccessManagement AuthorizedParties
        /// </summary>
        public const string AccessManagementAuthorizedParties = "AccessManagementAuthorizedParties";

        /// <summary>
        /// Feature flag for whether authorization of SystemUsers should include authorization through access packages
        /// </summary>
        public const string SystemUserAccessPackageAuthorization = nameof(SystemUserAccessPackageAuthorization);
    }
}
