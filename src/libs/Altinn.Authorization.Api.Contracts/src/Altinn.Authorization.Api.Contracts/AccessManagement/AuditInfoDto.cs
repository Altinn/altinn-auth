namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// When an access, delegation or request was last changed, and by whom.
/// Meant to be reused by every enduser API that shows this, not only permissions.
/// </summary>
public class AuditInfoDto
{
    /// <summary>
    /// When it was last changed: the time it was given, or the last time it was updated.
    /// It is not the original time it was given when it has been updated since.
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// The party that made the last change. May be a system entity, such as an import job.
    /// <c>null</c> when the party is not found.
    /// </summary>
    public AuditEntityDto? LastModifiedBy { get; set; }
}
