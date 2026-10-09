using System.Security.Claims;
using AltinnCore.Authentication.Constants;

namespace Altinn.Platform.Authenticaiton.Extensions
{
    /// <summary>
    /// Helper methods to extend ClaimsPrincipal. 
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the userId or the orgNumber or null if neither claims are present.
        /// </summary>
        public static string GetUserOrOrgId(this ClaimsPrincipal user)
        {
            int? userId = GetUserIdAsInt(user);
            if (userId.HasValue)
            {
                return userId.Value.ToString();
            }

            int? orgId = GetOrgNumber(user);
            if (orgId.HasValue)
            {
                return orgId.Value.ToString();
            }

            return null;
        }

        /// <summary>
        /// Get the org identifier string or null if it is not an org.
        /// </summary>        
        public static string GetOrg(this ClaimsPrincipal user)
        {
            if (user.HasClaim(c => c.Type == AltinnCoreClaimTypes.Org))
            {
                Claim orgClaim = user.FindFirst(c => c.Type == AltinnCoreClaimTypes.Org);
                if (orgClaim != null)
                {
                    return orgClaim.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the organisation number of an org user or null if claim does not exist.
        /// </summary>
        public static int? GetOrgNumber(this ClaimsPrincipal user)
        {
            if (user.HasClaim(c => c.Type == AltinnCoreClaimTypes.OrgNumber))
            {
                Claim orgClaim = user.FindFirst(c => c.Type == AltinnCoreClaimTypes.OrgNumber);
                if (orgClaim != null && int.TryParse(orgClaim.Value, out int orgNumber))
                {
                    return orgNumber;
                }
            }

            return null;
        }

        /// <summary>
        /// Return the userId as an int or null if UserId claim is not set
        /// </summary>
        public static int? GetUserIdAsInt(this ClaimsPrincipal user)
        {
            if (user.HasClaim(c => c.Type == AltinnCoreClaimTypes.UserId))
            {
                Claim userIdClaim = user.FindFirst(c => c.Type == AltinnCoreClaimTypes.UserId);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if the user has the given scope. Scopes are read from the
        /// <c>urn:altinn:scope</c> claim (federation identity) or the <c>scope</c> claim, and are space separated.
        /// </summary>
        public static bool HasScope(this ClaimsPrincipal user, string scope)
        {
            if (user == null || string.IsNullOrWhiteSpace(scope))
            {
                return false;
            }

            string contextScope = user.Identities
                ?.FirstOrDefault(i => i.AuthenticationType != null && i.AuthenticationType.Equals("AuthenticationTypes.Federation"))?.Claims
                .Where(c => c.Type.Equals("urn:altinn:scope"))?
                .Select(c => c.Value).FirstOrDefault();

            contextScope ??= user.Claims.Where(c => c.Type.Equals("scope")).Select(c => c.Value).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(contextScope))
            {
                return false;
            }

            return contextScope.Split(' ').Contains(scope);
        }

        /// <summary>
        /// Returns the authentication level of the user.
        /// </summary>
        public static int GetAuthenticationLevel(this ClaimsPrincipal user)
        {
            if (user.HasClaim(c => c.Type == AltinnCoreClaimTypes.AuthenticationLevel))
            {
                Claim userIdClaim = user.FindFirst(c => c.Type == AltinnCoreClaimTypes.AuthenticationLevel);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int authenticationLevel))
                {
                    return authenticationLevel;
                }
            }

            return 0;
        }
    }
}
