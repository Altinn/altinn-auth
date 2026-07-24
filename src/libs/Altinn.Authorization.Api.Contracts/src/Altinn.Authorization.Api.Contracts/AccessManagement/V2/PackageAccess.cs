using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.V2;

/// <summary>
/// Model representing package access granted through a single role. The role is what grants and ties together
/// all the package accesses listed within the same model.
/// </summary>
public class PackageAccess
{
    /// <summary>
    /// Gets or sets the role granting the packages listed in this model.
    /// </summary>
    [JsonPropertyName("role")]
    public CompactRoleDto Role { get; set; }

    /// <summary>
    /// Gets or sets the packages granted through <see cref="Role"/>.
    /// </summary>
    [JsonPropertyName("packages")]
    public List<CompactPackageDto> Packages { get; set; } = [];
}
