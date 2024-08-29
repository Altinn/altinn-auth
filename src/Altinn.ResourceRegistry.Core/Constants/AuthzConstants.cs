namespace Altinn.ResourceRegistry.Core.Constants
{
    /// <summary>
    /// Constants related to authorization
    /// </summary>
    public static class AuthzConstants
    {
        /// <summary>
        /// Policy tag for authorizing designer access
        /// </summary>
        public const string POLICY_STUDIO_DESIGNER = "StudioDesignerAccess";

        /// <summary>
        /// Policy tag for authorizing write scope.
        /// </summary>
        public const string POLICY_SCOPE_RESOURCEREGISTRY_WRITE = "ScopeResourceRegistryWrite";

        /// <summary>
        /// Policy tag for authorizing read/write maskinporten scope.
        /// </summary>
        public const string POLICY_SCOPE_RESOURCEREGISTRY_READ = "ScopeResourceRegistryRead";

        /// <summary>
        /// Policname for authorizing write access to access lists.
        /// </summary>
        public const string POLICY_ACCESS_LIST_WRITE = "AccessListWrite";

        /// <summary>
        /// Policy name for authorizing read access to access lists.
        /// </summary>
        public const string POLICY_ACCESS_LIST_READ = "AccessListRead";

        /// <summary>
        /// Policy name for authorizing endpoints that require admin privileges.
        /// </summary>
        public const string POLICY_ADMIN = "Admin";

        /// <summary>
        /// Scope required for PDP Access List
        /// </summary>
        public const string POLICY_ACCESS_LIST_PDP = "AccessListPDP";

        /// <summary>
        /// Scope for resourceregistry read access
        /// </summary>
        public const string SCOPE_RESOURCE_READ = "altinn:resourceregistry/resource.read";

        /// <summary>
        /// Scope for resource registry write access
        /// </summary>
        public const string SCOPE_RESOURCE_WRITE = "altinn:resourceregistry/resource.write";

        /// <summary>
        /// Scope for resource registry admin access
        /// </summary>
        public const string SCOPE_RESOURCE_ADMIN = "altinn:resourceregistry/resource.admin";

        /// <summary>
        /// Scope for access list read access
        /// </summary>
        public const string SCOPE_ACCESS_LIST_READ = "altinn:resourceregistry/accesslist.read";

        /// <summary>
        /// Scope for access list write access
        /// </summary>
        public const string SCOPE_ACCESS_LIST_WRITE = "altinn:resourceregistry/accesslist.write";

        /// <summary>
        /// Scope for access list info for PDP
        /// </summary>
        public const string SCOPE_ACCESS_LIST_PDP = "altinn:resourceregistry/pdp:accesslist.read";

        /// <summary>
        /// Claim for scopes from maskinporten token
        /// </summary>
        public const string CLAIM_MASKINPORTEN_SCOPE = "scope";

        /// <summary>
        /// Claim for consumer prefixes from maskinporten token
        /// </summary>
        public const string CLAIM_MASKINPORTEN_CONSUMER_PREFIX = "consumer_prefix";
    }
}
