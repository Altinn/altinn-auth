using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended AssignmentPackage
/// </summary>
public class AssignmentResource : BaseAssignmentResource
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
