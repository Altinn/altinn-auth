using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended EntityType
/// </summary>
public class EntityType : BaseEntityType
{
    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }
}
