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

        /// <summary>
        /// Feature flag for whether authorization of users should include authorization through access packages
        /// </summary>
        public const string UserAccessPackageAuthorization = nameof(UserAccessPackageAuthorization);

        /// <summary>
        /// Feature flag for whether single decision requests should be logged on error
        /// </summary>
        public const string DecisionRequestLogRequestOnError = nameof(DecisionRequestLogRequestOnError);

        /// <summary>
        /// Feature flag for whether decision multi requests should be logged on error
        /// </summary>
        public const string DecisionRequestLogRequestOnErrorMultiRequest = nameof(DecisionRequestLogRequestOnErrorMultiRequest);
    }
}
