using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Core.Configuration
{
    /// <summary>
    /// Settings for the open metadata API in Samarbeidsportalen (DigDir), used to look up
    /// which organization owns a Maskinporten scope prefix.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SamarbeidsportalenSettings
    {
        /// <summary>
        /// Full URL to the prefix list, for example <c>https://api.samarbeid.digdir.no/api/v1/prefix/all</c>.
        /// Test environments (AT/TT02) use the Test URL, PROD uses the production URL.
        /// </summary>
        public string PrefixEndpoint { get; set; }

        /// <summary>
        /// How long a fetched prefix list is used before it is refreshed. Requests between
        /// <see cref="PrefixCacheStaleAge"/> and <see cref="PrefixCacheExpiredAge"/> still get the
        /// cached list while it is refreshed in the background.
        /// </summary>
        public TimeSpan PrefixCacheStaleAge { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// How long a fetched prefix list may be used at all. After this, requests wait for a
        /// new list, and fail if it cannot be fetched.
        /// </summary>
        public TimeSpan PrefixCacheExpiredAge { get; set; } = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(15);
    }
}
