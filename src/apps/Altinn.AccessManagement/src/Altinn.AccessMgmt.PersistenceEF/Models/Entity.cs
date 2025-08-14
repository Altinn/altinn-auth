using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Entity
/// </summary>
public class Entity : BaseEntity
{
    /// <summary>
    /// Type
    /// </summary>
    public EntityType Type { get; set; }

    /// <summary>
    /// Variant
    /// </summary>
    public EntityVariant Variant { get; set; }

    /// <summary>
    /// Parent
    /// </summary>
    public Entity Parent { get; set; }
}
