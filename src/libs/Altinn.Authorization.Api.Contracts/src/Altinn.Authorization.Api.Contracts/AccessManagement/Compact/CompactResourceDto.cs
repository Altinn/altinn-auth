using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact version of resource
/// </summary>
public class CompactResourceDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Reference identifier
    /// </summary>
    [JsonPropertyName("refId")]
    public string RefId { get; set; }
}
