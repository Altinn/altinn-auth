using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing a connected agent party with the access packages delegated per role.
/// </summary>
public class AgentPackagesDto
{
    /// <summary>
    /// Gets or sets the party
    /// </summary>
    [JsonPropertyName("agent")]
    public CompactEntityDto Agent { get; set; }

    /// <summary>
    /// Specifies when the <see cref="Agent"/> was added.
    /// </summary>
    [JsonPropertyName("agentAddedAt")]
    public DateTimeOffset AgentAddedAt { get; set; }

    /// <summary>
    /// Gets or sets a collection of all access information for the agent
    /// </summary>
    [JsonPropertyName("access")]
    public List<PackageAccess> Access { get; set; } = [];
}
