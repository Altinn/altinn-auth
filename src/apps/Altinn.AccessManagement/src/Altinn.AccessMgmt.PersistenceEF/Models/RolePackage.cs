using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended RolePackage
/// </summary>
public class RolePackage : BaseRolePackage
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
    public EntityVariant EntityVariant { get; set; }
}
