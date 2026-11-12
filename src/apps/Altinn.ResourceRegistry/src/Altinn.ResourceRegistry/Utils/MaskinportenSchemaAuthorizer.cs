using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Utils
{
    /// <summary>
    /// Helpers for the Maskinporten scopes on a resource
    /// </summary>
    public static class MaskinportenSchemaAuthorizer
    {
        /// <summary>
        /// Creates an error model for scopes whose prefix is not owned by the resource owner,
        /// according to the prefix list in Samarbeidsportalen
        /// </summary>
        /// <param name="invalidScopes">the scopes with prefixes the resource owner does not own</param>
        /// <returns>A complete error model, with the scopes under the <c>InvalidPrefix</c> key</returns>
        public static ValidationProblemDetails CreateErrorResponseInvalidPrefix(IReadOnlyList<string> invalidScopes)
        {
            ValidationProblemDetails error = new ValidationProblemDetails(new Dictionary<string, string[]> { { "InvalidPrefix", invalidScopes.ToArray() } })
            {
                Title = "Invalid scope prefix",
                Detail = "The resource owner does not own the prefix of the listed Maskinporten scopes",
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
    }
}
