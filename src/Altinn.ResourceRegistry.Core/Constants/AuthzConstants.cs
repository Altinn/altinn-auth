using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core.Constants
{
    /// <summary>
    /// Constants related to authorization
    /// </summary>
    public static class AuthzConstants
    {
        /// <summary>
        /// Policy tag for authorizing write scope.
        /// </summary>
        public const string POLICY_SCOPE_RESOURCEREGISTRY_WRITE = "ScopeResourceRegistryWrite";

        /// <summary>
        /// Policy tag for authorizing read/write maskinporten scope.
        /// </summary>
        public const string POLICY_SCOPE_RESOURCEREGISTRY_READ = "ScopeResourceRegistryRead";

        /// <summary>
        /// Scope for resourceregistry read access
        /// </summary>
        public const string SCOPE_RESOURCEREGISTRY_READ = "altinn:resourceregistry/resource.read";

        /// <summary>
        /// Scope for resource registry write access
        /// </summary>
        public const string SCOPE_RESOURCEREGISTRY_WRITE = "altinn:resourceregistry/resource.write";

        /// <summary>
        /// Scope for resource registry admin access
        /// </summary>
        public const string SCOPE_RESOURCEREGISTRY_ADMIN = "altinn:resourceregistry/resource.admin";

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
