namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Permission
/// </summary>
public class PermissionDto
{
    /// <summary>
    /// From party
    /// </summary>
    public CompactEntityDto From { get; set; }

    /// <summary>
    /// To party
    /// </summary>
    public CompactEntityDto To { get; set; }

    /// <summary>
    /// Via party
    /// </summary>
    public CompactEntityDto Via { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public CompactRoleDto Role { get; set; }

    /// <summary>
    /// Via role
    /// </summary>
    public CompactRoleDto ViaRole { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public AccessReason Reason { get; set; }

    /// <summary>
    /// When this access was last changed, and by whom. Returned both for access given to others and access received
    /// from others. <c>null</c> for role packages, for resources that come through a package, for client delegations,
    /// and while the feature is turned off.
    /// </summary>
    public AuditInfoDto? AuditInfo { get; set; }
}
