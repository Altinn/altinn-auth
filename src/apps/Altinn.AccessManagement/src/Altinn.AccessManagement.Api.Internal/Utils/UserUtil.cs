using System.Security.Claims;

namespace Altinn.AccessManagement.Api.Internal.Utils
{
    /// <summary>
    /// Utility class for user related operations
    /// </summary>
    public static class UserUtil
    {
        /// <summary>
        /// Returns partyUid claim value if present. Null if not
        /// </summary>
        public static Guid? GetUserUuid(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal != null && claimsPrincipal.Claims != null)
            {
                Claim? partyClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type.Equals("urn:altinn:party:uuid"));
                if (partyClaim != null && Guid.TryParse(partyClaim.Value, out Guid partyUid))
                {
                    return partyUid;
                }
            }

            return null;
        }    
    }
}
