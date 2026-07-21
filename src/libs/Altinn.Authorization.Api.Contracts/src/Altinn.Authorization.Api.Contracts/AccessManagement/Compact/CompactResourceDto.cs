using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact versjon of resource
/// </summary>
public class CompactResourceDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
