using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Core.Configuration
{
    /// <summary>
    /// Represents a set of configuration options when communicating with the platform API.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PlatformSettings
    {
        /// <summary>
        /// URL endpoint for AccessManagement API
        /// </summary>
        public string ApiAccessManagementEndpoint { get; set; }

        /// <summary>
        /// Issuer to use in the generated token that will be used in calling AccessManagement
        /// </summary>
        public string AccessTokenIssuer { get; set; } = "Platform";

        /// <summary>
        /// App to use in the generated token that will be used in calling AccessManagement
        /// </summary>
        public string AccessTokenApp { get; set; } = "ResourceRegistry";

        /// <summary>
        /// The headder value to use for AccessToken
        /// </summary>
        public string AccessTokenHeaderId { get; set; } = "PlatformAccessToken";

        /// <summary>
        /// Uri to Bridge API
        /// </summary>
        public string BridgeApiEndpoint { get; set; }

        /// <summary>
        /// Storage Api
        /// </summary>
        public string StorageApiEndpoint { get; set; }
    }
}
