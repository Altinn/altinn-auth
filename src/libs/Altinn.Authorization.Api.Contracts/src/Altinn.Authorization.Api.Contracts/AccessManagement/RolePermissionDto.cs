namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Role permissions
/// </summary>
public class RolePermissionDto
{
    /// <summary>
    /// Role the permissions are for
    /// </summary>
    public RoleDto Role { get; set; }

    /// <summary>
    /// Parties with permissions
    /// </summary>
    public IEnumerable<PermissionDto> Permissions { get; set; }
}
