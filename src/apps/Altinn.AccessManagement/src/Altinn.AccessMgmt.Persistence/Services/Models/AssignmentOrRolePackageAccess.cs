using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Represents a package access for a given Assignment through either AssignmentPackage or RolePackage
/// </summary>
public class AssignmentOrRolePackageAccess
{
    /// <summary>
    /// AssignmentId
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// RoleId
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// PackageId
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// AssignmentPackageId
    /// </summary>
    public Guid? AssignmentPackageId { get; set; }

    /// <summary>
    /// RolePackageId
    /// </summary>
    public Guid? RolePackageId { get; set; }
}
