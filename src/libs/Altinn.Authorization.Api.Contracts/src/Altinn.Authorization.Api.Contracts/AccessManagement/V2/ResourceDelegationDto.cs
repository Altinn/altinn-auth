using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Result of a single resource delegation operation between a client and an agent.
/// </summary>
public class ResourceDelegationDto
{
    /// <summary>
    /// Identifier of the role the resource is delegated through.
    /// </summary>
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Identifier of the delegated resource.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Identifier of the facilitator party the delegation goes through.
    /// </summary>
    [JsonPropertyName("viaId")]
    public Guid ViaId { get; set; }

    /// <summary>
    /// Identifier of the client party the resource is delegated from.
    /// </summary>
    [JsonPropertyName("fromId")]
    public Guid FromId { get; set; }

    /// <summary>
    /// Identifier of the agent party the resource is delegated to.
    /// </summary>
    [JsonPropertyName("toId")]
    public Guid ToId { get; set; }

    /// <summary>
    /// Indicates whether the request resulted in any state change.
    /// </summary>
    [JsonPropertyName("changed")]
    public bool Changed { get; set; }
}
