using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended EntityVariantRole
/// </summary>
public class EntityVariantRole : BaseEntityVariantRole
{
    /// <summary>
    /// Variant
    /// </summary>
    public EntityVariant Variant { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }
}
