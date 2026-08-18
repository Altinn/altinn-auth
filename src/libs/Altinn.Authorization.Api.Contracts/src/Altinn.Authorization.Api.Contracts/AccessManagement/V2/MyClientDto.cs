using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing the clients available to a party through a provider,
/// including the accesses delegated for each client.
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
