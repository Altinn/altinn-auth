using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended RoleMap
/// </summary>
public class RoleMap : BaseRoleMap
{
    /// <summary>
    /// HasRole (Role)
    /// </summary>
    public Role HasRole { get; set; }

    /// <summary>
    /// GetRole (Role)
    /// </summary>
    public Role GetRole { get; set; }
}
