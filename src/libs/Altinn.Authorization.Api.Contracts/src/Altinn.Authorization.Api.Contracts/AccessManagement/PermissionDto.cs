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
    /// The party that last changed this access: <c>id</c>, <c>name</c>, <c>type</c>, <c>variant</c>,
    /// <c>partyid</c>, <c>userId</c>, <c>username</c>, and <c>organizationIdentifier</c> for organizations. Never contains <c>personIdentifier</c> or <c>dateOfBirth</c>.
    /// May be a system entity, such as an import job. Returned both for access given to others and access received
    /// from others. <c>null</c> for role packages, for resources that come through a package, for client delegations,
    /// where no change is recorded, and while the feature is turned off.
    /// </summary>
    public CompactEntityDto? PerformedBy { get; set; }

    /// <summary>
    /// When this access was last changed: the time it was given, or the last time it was updated.
    /// It is not the original time it was given when the access has been updated since.
    /// <c>null</c> for role packages, for resources that come through a package, for client delegations,
    /// and while the feature is turned off.
    /// </summary>
    public DateTimeOffset? FromDate { get; set; }
}
