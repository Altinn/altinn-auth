using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Resource Scope
/// </summary>
public class ResourceScope : BaseResourceScope
{
    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}
