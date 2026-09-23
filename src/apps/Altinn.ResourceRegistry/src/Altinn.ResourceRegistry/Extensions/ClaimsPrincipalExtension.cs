using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using AltinnCore.Authentication.Constants;

namespace Altinn.ResourceRegistry.Extensions
{
    /// <summary>
    /// Helper methods to extend ClaimsPrincipal. 
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ClaimsPrincipalExtension
    {
        /// <summary>
        /// Get the org identifier string or null if it is not an org.
        /// </summary>        
        public static string GetOrgNumber(this ClaimsPrincipal User)
        {
            if (User.HasClaim(c => c.Type == AltinnCoreClaimTypes.OrgNumber))
            {
                Claim orgClaim = User.FindFirst(c => c.Type == AltinnCoreClaimTypes.OrgNumber);
                if (orgClaim != null)
                {
                    return orgClaim.Value;
                }
            }

            return null;
        }
    }
}
