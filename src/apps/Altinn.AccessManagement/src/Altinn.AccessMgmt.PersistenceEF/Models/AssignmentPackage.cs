using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended AssignmentPackage
/// </summary>
public class AssignmentPackage : BaseAssignmentPackage
{
    /// <summary>
    /// Assignment
    /// </summary>
    public Assignment Assignment { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }

    /// <summary>
    /// Lists of packages that are delegated further.
    /// </summary>
    public ICollection<DelegationPackage> DelegationPackages { get; set; } = [];
}
