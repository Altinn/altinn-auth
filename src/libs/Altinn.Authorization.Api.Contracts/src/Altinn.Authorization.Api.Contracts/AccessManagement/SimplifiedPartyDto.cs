using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Simplified party information for connections API responses
/// </summary>
public class SimplifiedPartyDto
{
    /// <summary>
    /// The unique identifier for the party
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the party
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The type of party (Person, Organization, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The variant/subtype of the party
    /// </summary>
    [JsonPropertyName("variant")]
    public string Variant { get; set; }

    /// <summary>
    /// Organization number (only for organizations)
    /// </summary>
    [JsonPropertyName("organizationIdentifier")]
    public string? OrganizationIdentifier { get; set; }

    /// <summary>
    /// Indicates if the party is deleted
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The timestamp when the party was deleted
    /// </summary>
    [JsonPropertyName("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; }
}
