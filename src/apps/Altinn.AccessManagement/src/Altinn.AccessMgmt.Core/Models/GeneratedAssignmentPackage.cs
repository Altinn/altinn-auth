namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Assignment
/// This is a computed view that combines assignments with rolemap
/// and connects assignment packages and role packages.
/// It also expands delegations and returns packages delegated.
/// Also: see GeneratedAssignmentResource and GeneratedAssignmentPackageResource
/// </summary>
public class GeneratedAssignmentPackage
{
    /// <summary>
    /// From entity identifier
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// Role identifier
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// To entity identifier
    /// </summary>
    public Guid ToId { get; set; }

    /// <summary>
    /// Package identifier
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// The entity that delegates from 'from' to 'to'
    /// </summary>
    public Guid DelegateEntityId { get; set; }

    /// <summary>
    /// The role used from Via to To
    /// </summary>
    public Guid DelegateRoleId { get; set; }

    /// <summary>
    /// Describes the source for the package
    /// Direct, Role, Delegated
    /// </summary>
    public string Description { get; set; }
}

/*
public class ExtGeneratedAssignmentPackage : GeneratedAssignmentPackage
{
    public Entity From { get; set; }
    public Role Role { get; set; }
    public Entity To { get; set; }
    public Package Package { get; set; }
    public Entity DelegateEntity { get; set; }
    public Role DelegateRole { get; set; }
}
*/
