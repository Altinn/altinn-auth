using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Simplified connection for listing available users
/// </summary>
public class SimplifiedConnectionDto
{
    /// <summary>
    /// The party information
    /// </summary>
    [JsonPropertyName("party")]
    public SimplifiedPartyDto Party { get; set; }

    /// <summary>
    /// Sub-connections (nested users under this party)
    /// </summary>
    [JsonPropertyName("connections")]
    public List<SimplifiedConnectionDto> Connections { get; set; } = new();
}
