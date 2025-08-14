namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// EntityVariantRole
/// </summary>
public class EntityVariantRole
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// VariantId
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// RoleId
    /// </summary>
    public Guid RoleId { get; set; }
}

/// <summary>
/// Extended EntityVariantRole
/// </summary>
public class ExtEntityVariantRole : EntityVariantRole
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

/// <summary>
/// Extended EntityVariantRole
/// </summary>
public class ExtendedEntityVariantRole : EntityVariantRole
{
    /// <summary>
    /// Variant
    /// </summary>
    public ExtendedEntityVariant Variant { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public ExtendedRole Role { get; set; }
}
