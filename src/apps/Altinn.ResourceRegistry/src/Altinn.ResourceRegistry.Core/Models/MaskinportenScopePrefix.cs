#nullable enable

using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// A Maskinporten scope prefix and the organization that owns it, as returned by
    /// <c>/api/v1/prefix/all</c> in Samarbeidsportalen.
    /// </summary>
    /// <remarks>
    /// Every property is optional so that one malformed record does not fail the whole list.
    /// Records without a prefix or owner are ignored when the list is used.
    /// </remarks>
    public record MaskinportenScopePrefix
    {
        /// <summary>
        /// The prefix, for example <c>altinn</c>. Scopes under it look like <c>altinn:some.scope</c>.
        /// </summary>
        [JsonPropertyName("prefix")]
        public string? Prefix { get; init; }

        /// <summary>
        /// Organization number of the owner of the prefix.
        /// </summary>
        [JsonPropertyName("owner_orgno")]
        public string? OwnerOrgNo { get; init; }

        /// <summary>
        /// Name of the owner. Missing on some records.
        /// </summary>
        [JsonPropertyName("organization_name")]
        public string? OrganizationName { get; init; }

        /// <summary>
        /// When the prefix was created.
        /// </summary>
        [JsonPropertyName("created")]
        public DateTimeOffset? Created { get; init; }

        /// <summary>
        /// When the prefix was last updated.
        /// </summary>
        [JsonPropertyName("last_updated")]
        public DateTimeOffset? LastUpdated { get; init; }

        /// <summary>
        /// Whether the prefix is active.
        /// </summary>
        [JsonPropertyName("active")]
        public bool Active { get; init; }
    }
}
