using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Model representing a connected client party, meaning a party which has been authorized for one or more accesses, either directly or through role(s), access packages, resources or resource instances.
/// Model can be used both to represent a connection received from another party or a connection provided to another party.
/// </summary>
public class MyClientDto
{
    /// <summary>
    /// Gets or sets the party
    /// </summary>
    [JsonPropertyName("provider")]
    public CompactEntityDto Provider { get; set; }

    /// <summary>
    /// All clients for given <see cref="Provider"/>.
    /// </summary>
    [JsonPropertyName("clients")]
    public IEnumerable<ClientDto> Clients { get; set; } = [];
}
