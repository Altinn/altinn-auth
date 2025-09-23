namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// RolePackage
/// </summary>
public class RolePackageDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// RoleId
    /// </summary>
    public RoleDto Role { get; set; }

    /// <summary>
    /// PackageId
    /// </summary>
    public PackageDto Package { get; set; }

    /// <summary>
    /// EntityVariantId (optional)
    /// </summary>
    public EntityVariantDto EntityVariant { get; set; }

    /// <summary>
    /// HasAccess
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// CanDelegate
    /// </summary>
    public bool CanDelegate { get; set; }
}
