#nullable enable

using System.Security.Claims;
using System.Text.Json;
using Altinn.Platform.Authorization.Telemetry;

namespace Altinn.Platform.Authorization.Helpers
{
    /// <summary>
    /// Resolves which kind of caller is behind a request to the external PDP API, so that PDP usage
    /// can be attributed for cost allocation.
    /// </summary>
    /// <remarks>
    /// The resource owner is normally the party that should carry the cost of a decision, but some
    /// callers evaluate access to resources they do not own. Digdir does this on behalf of the
    /// formidlingstjenester, and the resource owner (e.g. Skatteetaten) should not be billed for it.
    /// Classifying the caller lets the billing query separate those calls without adding an unbounded
    /// consumer dimension to the metric.
    /// </remarks>
    public static class PdpCallerHelper
    {
        /// <summary>
        /// The <c>consumer</c> claim as issued by Maskinporten. It survives the Altinn token exchange
        /// unmodified, so it is the one caller identifier available both on an exchanged Altinn token
        /// and on a raw Maskinporten token, should the PDP later accept those directly.
        /// </summary>
        private const string ConsumerClaimName = "consumer";

        /// <summary>
        /// The only authority accepted in the consumer claim, per ISO 6523. Norwegian organization
        /// numbers are carried with the <c>0192:</c> prefix.
        /// </summary>
        private const string Iso6523Authority = "iso6523-actorid-upis";

        /// <summary>
        /// Consumers that evaluate access to resources owned by others, mapped to the dimension value
        /// they are reported under. Deliberately a code constant rather than configuration: the set is
        /// known and effectively static, and a config section would add per-environment setup and a
        /// failure mode without buying anything.
        /// <para>
        /// Each entry gets its own dimension value rather than a shared "common" bucket, so that a
        /// future addition is distinguishable from Digdir in historical data at no extra cardinality
        /// cost. Digdir uses the same organization number in test and production.
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, string> CrossOwnerConsumers = new(StringComparer.Ordinal)
        {
            ["991825827"] = DecisionTelemetry.DigdirCallerDimensionValue,
        };

        /// <summary>
        /// Classifies the caller behind a request to the external PDP API.
        /// </summary>
        /// <param name="user">The authenticated caller.</param>
        /// <returns>
        /// The dimension value for a known cross-owner consumer, otherwise
        /// <see cref="DecisionTelemetry.OwnerCallerDimensionValue"/>.
        /// </returns>
        public static string GetExternalCallerKind(ClaimsPrincipal? user)
        {
            string? consumerOrgNumber = GetConsumerOrgNumber(user);

            if (consumerOrgNumber is not null
                && CrossOwnerConsumers.TryGetValue(consumerOrgNumber, out string? callerKind))
            {
                return callerKind;
            }

            return DecisionTelemetry.OwnerCallerDimensionValue;
        }

        /// <summary>
        /// Reads the organization number out of the <c>consumer</c> claim, which carries an ISO 6523
        /// identifier on the form <c>{"authority":"iso6523-actorid-upis","ID":"0192:991825827"}</c>.
        /// </summary>
        /// <returns>The organization number, or null if the claim is absent or malformed.</returns>
        private static string? GetConsumerOrgNumber(ClaimsPrincipal? user)
        {
            string? consumerJson = user?.FindFirst(ConsumerClaimName)?.Value;
            if (string.IsNullOrWhiteSpace(consumerJson))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(consumerJson);
                JsonElement root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("authority", out JsonElement authority)
                    || !root.TryGetProperty("ID", out JsonElement id)
                    || authority.ValueKind != JsonValueKind.String
                    || id.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                if (!string.Equals(authority.GetString(), Iso6523Authority, StringComparison.Ordinal))
                {
                    return null;
                }

                string[] identifierParts = id.GetString()!.Split(':');
                if (identifierParts.Length != 2 || string.IsNullOrWhiteSpace(identifierParts[1]))
                {
                    return null;
                }

                return identifierParts[1];
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
