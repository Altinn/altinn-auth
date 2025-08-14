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
}
