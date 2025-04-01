namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Packages added to an assignment
/// </summary>
public class AssignmentPackage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignmentPackage"/> class.
    /// </summary>
    public AssignmentPackage()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Assignment identity
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Package identifier
    /// </summary>
    public Guid PackageId { get; set; }
}

/// <summary>
/// Extended AssignmentPackage
/// </summary>
public class ExtAssignmentPackage : AssignmentPackage
{
    /// <summary>
    /// Assignment
    /// </summary>
    public Assignment Assignment { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }
}
