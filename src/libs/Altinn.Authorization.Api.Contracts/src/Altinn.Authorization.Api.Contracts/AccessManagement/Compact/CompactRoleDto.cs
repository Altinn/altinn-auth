using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact Role Model
/// </summary>
public class CompactRoleDto
{
    /// <summary>
    /// Id
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// Children
    /// </summary>
    [JsonPropertyName("children")]
    public List<CompactRoleDto> Children { get; set; }
}
