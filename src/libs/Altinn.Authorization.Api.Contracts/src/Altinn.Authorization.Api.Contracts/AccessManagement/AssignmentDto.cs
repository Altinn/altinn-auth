using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement;

public class AssignmentDto
{
    /// <summary>
    /// Id
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// RoleId
    /// </summary>
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// FromId
    /// </summary>
    [JsonPropertyName("fromId")]
    public Guid FromId { get; set; }

    /// <summary>
    /// ToId
    /// </summary>
    [JsonPropertyName("toId")]
    public Guid ToId { get; set; }
}
