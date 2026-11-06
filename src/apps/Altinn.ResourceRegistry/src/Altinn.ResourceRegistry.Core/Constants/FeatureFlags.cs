namespace Altinn.ResourceRegistry.Core.Constants
{
    /// <summary>
    /// Names of the feature flags used by the resource registry.
    /// </summary>
    public static class FeatureFlags
    {
        /// <summary>
        /// Check the prefixes of Maskinporten scopes on a resource against the Samarbeidsportalen
        /// prefix API, for all callers, instead of against the caller's token claims.
        /// The flag and the claim-based check are removed in #4341.
        /// </summary>
        public const string ValidateScopePrefixWithMaskinportenApi = "ResourceRegistry.ValidateScopePrefixWithMaskinportenApi";
    }
}
