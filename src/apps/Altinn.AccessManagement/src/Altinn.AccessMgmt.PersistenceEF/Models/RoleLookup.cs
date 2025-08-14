using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended role lookup
/// </summary>
public class RoleLookup : BaseRoleLookup
{
    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }
}
