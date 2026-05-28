using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Core.Configuration
{
    /// <summary>
    /// Represents a set of configuration options when communicating with the platform API.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ResourceRegistrySettings
    {
        /// <summary>
        /// Enables fetching Altinn 2 resources via SBL bridge.
        /// </summary>
        public bool EnableAltinn2Resources { get; set; } = true;

        /// <summary>
        /// URL to Orglist on CDN
        /// </summary>
        public string OrgListEndpoint { get; set; }

        /// <summary>
        /// Fallback Url for the OrgList
        /// </summary>
        public string OrgListAlternativeEndpoint { get; set; }
    }
}
