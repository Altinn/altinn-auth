namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// RolePackage
/// </summary>
public class RolePackage
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// RoleId
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// PackageId
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// EntityVariantId (optional)
    /// </summary>
    public Guid? EntityVariantId { get; set; }

    /// <summary>
    /// HasAccess
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// CanDelegate
    /// </summary>
    public bool CanDelegate { get; set; }
}

/// <summary>
/// Extended RolePackage
/// </summary>
public class ExtRolePackage : RolePackage
{
    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }

    /// <summary>
    /// Variant (optional)
    /// </summary>
    public EntityVariant? EntityVariant { get; set; }
}
