using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

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
    public List<ConnectionPermission> Permissions { get; set; }
}
