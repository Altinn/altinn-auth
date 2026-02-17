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

    /// <summary>
    /// Policy path refers to where the corresponding policy is stored
    /// </summary>
    public string PolicyPath { get; set; }

    /// <summary>
    /// The version of the policy referred to by this change.
    /// </summary>
    public string PolicyVersion { get; set; }
}
