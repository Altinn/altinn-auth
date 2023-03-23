using System.Security.Claims;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Utils
{
    /// <summary>
    /// Authorization helper for custom authorization for maskinporten schema API operations
    /// </summary>
    public static class MaskinportenSchemaAuthorizer
    {
        /// <summary>
        /// Authorization of whether the provided claims is authorized for lookup of delegations of the given scope
        /// </summary>
        /// <param name="scope">The scope to authorize for delegation lookup</param>
        /// <param name="claims">The claims principal of the authenticated organization</param>
        /// <param name="missingScopes">The scope missing prefix to handle empty list if all ok</param>
        /// <returns>bool</returns>
        public static bool IsAuthorizedForChangeResourceWithScopes(List<string> scope, ClaimsPrincipal claims, out List<string> missingScopes)
        {
            if (HasDelegationsAdminScope(claims))
            {
                missingScopes = new List<string>();
                return true;
            }

            return HasAuthorizedScopePrefixClaim(scope, claims, out missingScopes);
        }

        /// <summary>
        /// Creates an error model for missing access to certain scope prefixes
        /// </summary>
        /// <param name="forbiddenScopes">the list of scopes with prefixes not valid</param>
        /// <returns>A complete error model</returns>
        public static ValidationProblemDetails CreateErrorResponseMissingPrefix(List<string> forbiddenScopes)
        {
            var errorContent = forbiddenScopes is { Count: > 0 } ? new Dictionary<string, string[]> { { "InvalidPrefix", forbiddenScopes.ToArray() } } : new Dictionary<string, string[]>();

            ValidationProblemDetails error = new ValidationProblemDetails(errorContent)
            {
                Title = "Unauthorized",
                Detail = "Not authorized for creating resource with the listed scopes",
            };

            return error;
        }

        /// <summary>
        /// Returns a list of Maskinporten scopes from a ServiceResource
        /// </summary>
        /// <param name="serviceResource">The ServiceResource to fetch scopes from</param>
        /// <returns>null if the ServiceResource has no ResourceReferences, an empty list if the list contains no Maskinporten scopes or a list of the existing scopes</returns>
        public static List<string> GetMaskinportenScopesFromServiceResource(ServiceResource serviceResource)
        {
            return serviceResource.ResourceReferences?.FindAll(r => r.ReferenceType == ReferenceType.MaskinportenScope && r.Reference != null).Select(r => r.Reference).ToList();
        }

        private static bool HasDelegationsAdminScope(ClaimsPrincipal claims)
        {
            return HasScope(claims, AuthzConstants.SCOPE_RESOURCEREGISTRY_ADMIN);
        }

        private static bool HasScope(ClaimsPrincipal claims, string scope)
        {
            Claim c = claims.Claims.FirstOrDefault(x => x.Type == AuthzConstants.CLAIM_MASKINPORTEN_SCOPE);
            if (c == null)
            {
                return false;
            }

            string[] scopes = c.Value.Split(' ');

            return scopes.Contains(scope);
        }

        private static bool HasAuthorizedScopePrefixClaim(List<string> scopesToAuthorize, ClaimsPrincipal claims, out List<string> missingScopes)
        {
            List<string> prefixes = claims.Claims.Where(x => x.Type == AuthzConstants.CLAIM_MASKINPORTEN_CONSUMER_PREFIX).Select(v => v.Value).ToList();
            missingScopes = scopesToAuthorize.FindAll(scope => !prefixes.Any(prefix => scope.StartsWith(prefix + ':')));

            return missingScopes.Count == 0;
        }
    }
}
