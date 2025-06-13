using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.AccessManagement.Core.Constants;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessManagement.Core.Helpers
{
    /// <summary>
    /// helper class for authentication
    /// </summary>
    public static class AuthenticationHelper
    {
        /// <summary>
        /// Gets the users id
        /// </summary>
        /// <param name="context">the http context</param>
        /// <returns>the logged in users id</returns>
        public static int GetUserId(HttpContext context)
        {
            var claim = context.User?.Claims.FirstOrDefault(c => c.Type.Equals(AltinnCoreClaimTypes.UserId));
            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }

            return 0;
        }

        /// <summary>
        /// Gets the users PartyUuid
        /// </summary>
        /// <param name="context">the http context</param>
        /// <returns>the logged in users party uuid</returns>
        public static Guid GetPartyUuid(HttpContext context)
        {
            var claim = context.User?.Claims.FirstOrDefault(c => c.Type.Equals(AltinnCoreClaimTypes.PartyUuid));
            if (claim != null && Guid.TryParse(claim.Value, out Guid userUuid))
            {
                return userUuid;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Gets the authenticated user's party id
        /// </summary>
        /// <param name="context">the http context</param>
        /// <returns>the logged in users party id</returns>
        public static int GetPartyId(HttpContext context)
        {
            var claim = context.User?.Claims.FirstOrDefault(c => c.Type.Equals(AltinnCoreClaimTypes.PartyID));
            if (claim != null && int.TryParse(claim.Value, out int partyId))
            {
                return partyId;
            }

            return 0;
        }

        /// <summary>
        /// Gets the system user PartyUuid
        /// </summary>
        /// <param name="context">the http context</param>
        /// <returns>The sytem user uuid</returns>
        public static string GetSystemUserUuid(HttpContext context)
        {
            var claim = context.User?.Claims.FirstOrDefault(c => c.Type.Equals("authorization_details"));
            if (claim == null || string.IsNullOrEmpty(claim.Value))
            {
                return string.Empty;
            }

            AuthorizationDetails authDetails = JsonSerializer.Deserialize<AuthorizationDetails>(claim.Value);
            if (authDetails != null && authDetails.Type == "urn:altinn:systemuser")
            {
                return authDetails.SystemUserId[0];
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the users authentication level
        /// </summary>
        /// <param name="context">the http context</param>
        /// <returns>the logged in users authentication level</returns>
        public static int GetUserAuthenticationLevel(HttpContext context)
        {
            var claim = context.User?.Claims.FirstOrDefault(c => c.Type.Equals(AltinnCoreClaimTypes.AuthenticationLevel));
            if (claim != null && int.TryParse(claim.Value, out int authenticationLevel))
            {
                return authenticationLevel;
            }

            return 0;
        }

        /// <summary>
        /// AuthorizationDetails class
        /// </summary>
        public class AuthorizationDetails
        {
            /// <summary>
            /// Type
            /// </summary>
            [JsonPropertyName("type")]
            public string Type { get; set; }

            /// <summary>
            /// SystemUserId
            /// </summary>
            [JsonPropertyName("systemuser_id")]
            public string[] SystemUserId { get; set; }

            /// <summary>
            /// SystemUserOrg
            /// </summary>
            [JsonPropertyName("systemuser_org")]
            public SystemUserOrg SystemUserOrg { get; set; }

            /// <summary>
            /// SystemId
            /// </summary>
            [JsonPropertyName("system_id")]
            public string SystemId { get; set; }
        }

        /// <summary>
        /// SystemUserOrg class
        /// </summary>
        public class SystemUserOrg
        {
            /// <summary>
            /// Authority
            /// </summary>
            [JsonPropertyName("authority")]
            public string Authority { get; set; }

            /// <summary>
            /// ID
            /// </summary>
            [JsonPropertyName("ID")]
            public string Id { get; set; }
        }
    }
}
