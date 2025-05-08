using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

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
    public EntityVariant EntityVariant { get; set; }

    /// <summary>
    /// HasAccess
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// CanDelegate
    /// </summary>
    public bool CanDelegate { get; set; }

    /// <summary>
    /// Construct from RolePackage
    /// </summary>
    /// <param name="rolePackage"><see cref="RolePackage"/>Role</param>
    public RolePackageDto(ExtRolePackage rolePackage)
    {
        Id = rolePackage.Id;
        Role = new RoleDto(rolePackage.Role);
        Package = new PackageDto(rolePackage.Package);
        EntityVariant = rolePackage.EntityVariant;
        HasAccess = rolePackage.HasAccess;
        CanDelegate = rolePackage.CanDelegate;
    }
}
