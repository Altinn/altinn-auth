using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing access granted through a single role. The role is what grants and ties together
/// all the package and resource accesses listed within the same model.
/// </summary>
public class RoleAccess
{
    /// <summary>
    /// Gets or sets the role granting the packages and resources listed in this model.
    /// </summary>
    [JsonPropertyName("role")]
    public CompactRoleDto Role { get; set; }

    /// <summary>
    /// Gets or sets the packages granted through <see cref="Role"/>.
    /// </summary>
    [JsonPropertyName("packages")]
    public List<CompactPackageDto> Packages { get; set; } = [];

    /// <summary>
    /// Gets or sets the resources granted through <see cref="Role"/>.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<CompactResourceDto> Resources { get; set; } = [];
}
