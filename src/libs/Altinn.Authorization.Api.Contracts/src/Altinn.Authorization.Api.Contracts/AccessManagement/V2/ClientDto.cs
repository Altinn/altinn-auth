using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing a connected client party, meaning a party which has been authorized for one or more accesses, either directly or through role(s), access packages, resources or resource instances.
/// Model can be used both to represent a connection received from another party or a connection provided to another party.
/// </summary>
public class ClientDto
{
    /// <summary>
    /// Gets or sets the party
    /// </summary>
    [JsonPropertyName("client")]
    public CompactEntityDto Client { get; set; }

    /// <summary>
    /// Gets or sets a collection of all access information for the client
    /// </summary>
    [JsonPropertyName("access")]
    public List<RoleAccess> Access { get; set; } = [];
}
