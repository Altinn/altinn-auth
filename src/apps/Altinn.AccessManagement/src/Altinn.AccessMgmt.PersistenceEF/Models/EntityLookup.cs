using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Entity Lookup
/// </summary>
public class EntityLookup : BaseEntityLookup
{
    /// <summary>
    /// Entity
    /// </summary>
    public Entity Entity { get; set; }
}
