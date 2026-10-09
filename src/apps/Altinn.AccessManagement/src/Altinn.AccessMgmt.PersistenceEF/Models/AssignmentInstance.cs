using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended AssignmentPackage
/// </summary>
public class AssignmentInstance : BaseAssignmentInstance
{
    /// <summary>
    /// Assignment
    /// </summary>
    public Assignment Assignment { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }

    /// <summary>
    /// Gets or sets the source type of the instance delegation, indicating the origin of the data.
    /// </summary>
    public InstanceSourceType InstanceSourceType { get; set; }
}
