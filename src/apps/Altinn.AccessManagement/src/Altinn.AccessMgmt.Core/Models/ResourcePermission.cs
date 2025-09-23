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
    public Resource Resource { get; set; }

    /// <summary>
    /// Parties with permissions
    /// </summary>
    public IEnumerable<PermissionDto> Permissions { get; set; }
}
