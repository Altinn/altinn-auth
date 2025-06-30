using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Api.Enterprise.Utils
{
    /// <summary>
    /// Utility class for user related operations
    /// </summary>
    public static class OrgUtil
    {
        /// <summary>
        /// Returns the scopes for the Maskinporten client from the claims principal.
        /// </summary>
        public static string? GetMaskinportenScopes(ClaimsPrincipal claimsPrincipal)
        {
            string? scopes = claimsPrincipal.FindFirstValue("scope");
            return scopes;
        }

        /// <summary>
        /// Returns orgnumber for the consumer party from the claims principal.
        /// </summary>
        public static ConsentPartyUrn? GetAuthenticatedParty(ClaimsPrincipal claimsPrincipal)
        {
            string? consumerJson = claimsPrincipal.FindFirstValue("consumer");
            if (string.IsNullOrWhiteSpace(consumerJson))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(consumerJson);
                return GetOrg(doc);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Return supplier party from the claims principal.
        /// </summary>
        public static ConsentPartyUrn? GetSupplierParty(ClaimsPrincipal claimsPrincipal)
        {
            string? consumerJson = claimsPrincipal.FindFirstValue("supplier");
            if (string.IsNullOrWhiteSpace(consumerJson))
            {
                return null;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(consumerJson);
                return GetOrg(doc);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static ConsentPartyUrn? GetOrg(JsonDocument doc)
        {
            var root = doc.RootElement;

            if (!root.TryGetProperty("ID", out var idElement) ||
                !root.TryGetProperty("authority", out var authorityElement))
            {
                return null;
            }

            var consumerAuthority = authorityElement.GetString();
            if (!string.Equals(consumerAuthority, "iso6523-actorid-upis", StringComparison.Ordinal))
            {
                return null;
            }

            var consumerId = idElement.GetString();
            if (string.IsNullOrEmpty(consumerId) || !consumerId.Contains(':'))
            {
                return null;
            }

            var parts = consumerId.Split(':');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                return null;
            }

            var organisationNumber = parts[1];
            return ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(organisationNumber));
        }
    }
}
