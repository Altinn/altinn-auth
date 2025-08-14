using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended delegation resource
/// </summary>
public class DelegationResource : BaseDelegationResource
{
    /// <summary>
    /// Delegation
    /// </summary>
    public Delegation Delegation { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}
