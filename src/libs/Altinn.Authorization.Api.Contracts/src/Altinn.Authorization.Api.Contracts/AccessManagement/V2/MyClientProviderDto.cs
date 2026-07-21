using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing a provider seen from the agent's perspective, meaning a party which has assigned the Agent role to the authenticated user,
/// making the provider's clients available for delegation to the agent.
/// </summary>
public class MyClientProviderDto
{
    /// <summary>
    /// Gets or sets the provider party which has added the authenticated user as an agent
    /// </summary>
    [JsonPropertyName("provider")]
    public CompactEntityDto Provider { get; set; }

    /// <summary>
    /// Specifies when the <see cref="Provider"/> added the authenticated user as an agent.
    /// </summary>
    [JsonPropertyName("providerAddedAt")]
    public DateTimeOffset ProviderAddedAt { get; set; }

    /// <summary>
    /// Gets or sets a collection of all access information the agent has through the provider
    /// </summary>
    [JsonPropertyName("access")]
    public List<RoleAccess> Access { get; set; } = [];

    /// <summary>
    /// Access given through a single role
    /// </summary>
    public class RoleAccess
    {
        /// <summary>
        /// Role
        /// </summary>
        [JsonPropertyName("role")]
        public CompactRoleDto Role { get; set; }

        /// <summary>
        /// Packages
        /// </summary>
        [JsonPropertyName("packages")]
        public List<CompactPackageDto> Packages { get; set; } = [];

        /// <summary>
        /// Resources
        /// </summary>
        [JsonPropertyName("resources")]
        public List<CompactResourceDto> Resources { get; set; } = [];
    }
}
