using System.Text.Json.Serialization;

namespace Altinn.AccessManagement.Api.Enduser.Models;

/// <summary>
/// Assignment Model
/// </summary>
public class AssignmentExternal
{
    /// <summary>
    /// Identity
    /// </summary>
    [JsonPropertyName("Id")]
    public Guid Id { get; init; }

    /// <summary>
    /// RoleId
    /// </summary>
    [JsonPropertyName("RoleId")]
    public Guid RoleId { get; init; }

    /// <summary>
    /// FromId
    /// </summary>
    [JsonPropertyName("FromId")]
    public Guid FromId { get; init; }

    /// <summary>
    /// ToId
    /// </summary>
    [JsonPropertyName("ToId")]
    public Guid ToId { get; init; }
}
