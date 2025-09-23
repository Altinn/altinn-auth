using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Permission
/// </summary>
public class PermissionDto
{
    /// <summary>
    /// From party
    /// </summary>
    public Entity From { get; set; }

    /// <summary>
    /// To party
    /// </summary>
    public Entity To { get; set; }

    /// <summary>
    /// Via party
    /// </summary>
    public Entity Via { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Via role
    /// </summary>
    public Role ViaRole { get; set; }
}
