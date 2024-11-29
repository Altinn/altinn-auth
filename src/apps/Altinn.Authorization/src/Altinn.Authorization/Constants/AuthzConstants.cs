namespace Altinn.Platform.Authorization.Constants
{
    /// <summary>
    /// Constants related to authorization.
    /// </summary>
    public static class AuthzConstants
    {
        /// <summary>
        /// Policy tag for authorizing designer access
        /// </summary>
        public const string POLICY_STUDIO_DESIGNER = "StudioDesignerAccess";

        /// <summary>
        /// Policy tag for authorizing PlatformAccessTokens issued by Platform
        /// </summary>
        public const string POLICY_PLATFORMISSUER_ACCESSTOKEN = "PlatformIssuedAccessToken";

        /// <summary>
        /// The issuer of access tokens for the platform cluster
        /// </summary>
        public const string PLATFORM_ACCESSTOKEN_ISSUER = "platform";

        /// <summary>
        /// Policy tag for authorizing Altinn.Platform.Authorization API access from AltinnII Authorization
        /// </summary>
        public const string ALTINNII_AUTHORIZATION = "AltinnIIAuthorizationAccess";

        /// <summary>
        /// Policy tag for authorizing Altinn.Platform.Authorization API access from the DelegationEvent Azure function
        /// </summary>
        public const string DELEGATIONEVENT_FUNCTION_AUTHORIZATION = "DelegationEventFunctionAccess";

        /// <summary>
        /// Policy for scope access to Authorize API
        /// </summary>
        public const string AUTHORIZESCOPEACCESS = "AuthorizeScopeAccess";

        /// <summary>
        /// Scope that gives access to external Authorize API for service/resource owners
        /// </summary>
        public const string AUTHORIZE_SCOPE = "altinn:authorization/authorize";

        /// <summary>
        /// SScope that gives access to external Authorize API for decision requests across service/resource owners
        /// </summary>
        public const string AUTHORIZE_ADMIN_SCOPE = "altinn:authorization/authorize.admin";
    }
}
