using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Altinn.Common.PEP.Constants;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using AltinnCore.Authentication.Constants;
using Newtonsoft.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Altinn.ResourceRegistry.Utils
{
    /// <summary>
    /// Custom authorization
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AuthorizationUtil
    {
        /// <summary>
        /// Verifies a scope claim based on claimsprincipal.
        /// </summary>
        /// <param name="requiredScope">Requiered scope.</param>
        /// <param name="user">Claim principal from http context.</param>
        /// <returns>true if the given ClaimsPrincipal or on of its identities have contains the given scope.</returns>
        public static bool ContainsRequiredScope(List<string> requiredScope, ClaimsPrincipal user)
        {
            string contextScope = user.Identities?
               .FirstOrDefault(i => i.AuthenticationType != null && i.AuthenticationType.Equals("AuthenticationTypes.Federation"))
               ?.Claims
               .Where(c => c.Type.Equals("urn:altinn:scope"))
               ?.Select(c => c.Value).FirstOrDefault();

            contextScope ??= user.Claims.Where(c => c.Type.Equals("scope")).Select(c => c.Value).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(contextScope))
            {
                return requiredScope.Any(scope => contextScope.Contains(scope, StringComparison.InvariantCultureIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Checks if the authenticated user has admin scope
        /// </summary>
        /// <returns>true/false</returns>
        public static bool HasAdminAccess(ClaimsPrincipal user)
        {
            List<string> requiredScopes = new List<string>();
            requiredScopes.Add(AuthzConstants.SCOPE_RESOURCE_ADMIN);
            if (ContainsRequiredScope(requiredScopes, user))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if resource owner organisation matches organisation in claim
        /// </summary>
        /// <param name="resourceOwner">the organisation number that owns the resource</param>
        /// <param name="organisation">the authenticated organisation claim</param>
        /// <returns></returns>
        public static bool IsOwnerOfResource(string resourceOwner, ClaimsPrincipal organisation) 
        {
            Console.WriteLine($"AuthorizationUtil // IsOwnerOfResource // Checking organisation number in claims.");

            string orgClaim = organisation?.Claims.Where(c => c.Type.Equals("consumer")).Select(c => c.Value).FirstOrDefault();

            string orgNumber = GetOrganizationNumberFromClaim(orgClaim);

            Console.WriteLine($"AuthorizationUtil // IsOwnerOfResource // Org claim: {orgClaim}.");

            if (resourceOwner.Equals(orgNumber, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if authenticated user has write access
        /// </summary>
        /// <param name="resourceOwner">the organisation number that owns the resource</param>
        /// <param name="user">the authenticated user claim</param>
        /// <returns></returns>
        public static bool HasWriteAccess(string resourceOwner, ClaimsPrincipal user)
        {
            if (!HasAdminAccess(user) && !IsOwnerOfResource(resourceOwner, user))
            {
                return false;
            }

            return true;
        }

        private static string GetOrganizationNumberFromClaim(string claim) 
        {
            ConsumerClaim consumerClaim;
            try
            {
                consumerClaim = JsonConvert.DeserializeObject<ConsumerClaim>(claim);
            }
            catch (JsonReaderException)
            {
                throw new ArgumentException("Invalid consumer claim: invalid JSON");
            }

            if (consumerClaim.Authority != "iso6523-actorid-upis")
            {
                throw new ArgumentException("Invalid consumer claim: unexpected authority");
            }

            string[] identityParts = consumerClaim.Id.Split(':');
            if (identityParts[0] != "0192")
            {
                throw new ArgumentException("Invalid consumer claim: unexpected ISO6523 identifier");
            }

            return identityParts[1];
        }
    }
}
