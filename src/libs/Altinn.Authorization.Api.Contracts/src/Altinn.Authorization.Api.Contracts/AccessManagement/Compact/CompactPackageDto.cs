using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact Package Model
/// </summary>
public class CompactPackageDto
{
    /// <summary>
    /// Id
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Urn
    /// </summary>
    [JsonPropertyName("urn")]
    public string Urn { get; set; }

    /// <summary>
    /// AreaId
    /// </summary>
    [JsonPropertyName("areaId")]
    public Guid AreaId { get; set; }
}
