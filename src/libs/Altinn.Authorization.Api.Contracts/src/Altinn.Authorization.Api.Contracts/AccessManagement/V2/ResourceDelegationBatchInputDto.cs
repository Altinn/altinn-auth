using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Batch input for delegating or removing single resources between a client and an agent.
/// </summary>
public class ResourceDelegationBatchInputDto
{
    /// <summary>
    /// The role and resource combinations to delegate or remove.
    /// </summary>
    [JsonPropertyName("values")]
    public List<Permission> Values { get; set; } = [];

    /// <summary>
    /// Resources tied to a single role the client has assigned to the facilitator.
    /// </summary>
    public class Permission
    {
        /// <summary>
        /// Code, urn or identifier of the role the client has assigned to the facilitator.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }

        /// <summary>
        /// Resource registry identifiers of the resources to delegate or remove.
        /// </summary>
        [JsonPropertyName("resources")]
        public List<string> Resources { get; set; } = [];
    }
}
