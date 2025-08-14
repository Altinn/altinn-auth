using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Resource permission
/// </summary>
public class ResourcePermission
{
    /// <summary>
    /// Resource the permissions are for
    /// </summary>
    public CompactResource Resource { get; set; }

    /// <summary>
    /// Parties with permissions
    /// </summary>
    public IEnumerable<Permission> Permissions { get; set; }
}
