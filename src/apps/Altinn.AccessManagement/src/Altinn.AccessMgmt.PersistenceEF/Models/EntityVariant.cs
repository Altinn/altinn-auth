using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended EntityVariant
/// </summary>
public class EntityVariant : BaseEntityVariant
{
    /// <summary>
    /// Type
    /// </summary>
    public EntityType Type { get; set; }
}
