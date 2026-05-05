using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended AssignmentPackage
/// </summary>
public class AssignmentResource : BaseAssignmentResource
{
    /// <summary>
    /// The associated assignment
    /// </summary>
    public Assignment Assignment { get; set; }

    /// <summary>
    /// The associated resource
    /// </summary>
    public Resource Resource { get; set; }

    public Entity? ChangedBy { get; set; }
}
