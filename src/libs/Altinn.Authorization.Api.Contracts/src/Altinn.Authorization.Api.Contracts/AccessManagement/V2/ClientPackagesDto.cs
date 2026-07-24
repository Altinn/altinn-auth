using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing a connected client party with the access packages delegated per role.
/// </summary>
public class ClientPackagesDto
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
    public List<PackageAccess> Access { get; set; } = [];
}
