using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing a connected agent party with the resources delegated per role.
/// </summary>
public class AgentResourcesDto
{
    /// <summary>
    /// Gets or sets the party
    /// </summary>
    [JsonPropertyName("agent")]
    public CompactEntityDto Agent { get; set; }

    /// <summary>
    /// Gets or sets a collection of all access information for the agent
    /// </summary>
    [JsonPropertyName("access")]
    public List<ResourceAccess> Access { get; set; } = [];
}
