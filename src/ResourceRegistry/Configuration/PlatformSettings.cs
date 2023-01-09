using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Configuration
{
    /// <summary>
    /// General configuration settings
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PlatformSettings
    {
        /// <summary>
        /// Open Id Connect Well known endpoint
        /// </summary>
        public string? OpenIdWellKnownEndpoint { get; set; }

        /// <summary>
        /// Name of the cookie for where JWT is stored
        /// </summary>
        public string? JwtCookieName { get; set; }

        /// <summary>
        /// Endpoint for authentication
        /// </summary>
        public string? ApiAuthenticationEndpoint { get; set; }
    }
}
