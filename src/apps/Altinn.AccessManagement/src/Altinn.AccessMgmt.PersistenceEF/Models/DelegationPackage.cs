using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended delgation package
/// </summary>
public class DelegationPackage : BaseDelegationPackage
{
    /// <summary>
    /// Delegation
    /// </summary>
    public Delegation Delegation { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }

    /// <summary>
    /// Nav property for Assignmentpackage if delegation is thorugh role <see cref="RoleConstants.Rightholder"/>
    /// </summary>
    public AssignmentPackage? AssignmentPackage { get; set; }
}
