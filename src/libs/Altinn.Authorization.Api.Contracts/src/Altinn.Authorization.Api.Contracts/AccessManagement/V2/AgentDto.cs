using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing a connected agent party, meaning a party which has been authorized for one or more accesses, either directly or through role(s), access packages, resources or resource instances.
/// Model can be used both to represent a connection received from another party or a connection provided to another party.
/// </summary>
public class AgentDto
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
    public List<RoleAccess> Access { get; set; } = [];
}
