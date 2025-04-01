namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Packages added to an assignment
/// </summary>
public class AssignmentResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssignmentResource"/> class.
    /// </summary>
    public AssignmentResource()
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
    /// Resource identifier
    /// </summary>
    public Guid ResourceId { get; set; }
}

/// <summary>
/// Extended AssignmentPackage
/// </summary>
public class ExtAssignmentResource : AssignmentResource
{
    /// <summary>
    /// Assignment
    /// </summary>
    public Assignment Assignment { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}
