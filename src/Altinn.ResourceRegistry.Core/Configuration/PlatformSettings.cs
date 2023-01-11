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
        /// Base URL to AccessManagement
        /// </summary>
        public string AccessManagementEndpoint { get; set; }

        /// <summary>
        /// Issuer to use in the generated token that will be used in calling AccessManagement
        /// </summary>
        public string AccessTokenIssuer { get; set; } = "Platform";

        /// <summary>
        /// App to use in the generated token that will be used in calling AccessManagement
        /// </summary>
        public string AccessTokenApp { get; set; } = "ResourceRegister";

        /// <summary>
        /// The headder value to use for AccessToken
        /// </summary>
        public string AccessTokenHeaderId { get; set; } = "PlatformAccessToken";
    }
}
