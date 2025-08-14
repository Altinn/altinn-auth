using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Package permissions
/// </summary>
public class PackagePermission
{
    /// <summary>
    /// Package the permissions are for
    /// </summary>
    public CompactPackage Package { get; set; }

    /// <summary>
    /// Parties with permissions
    /// </summary>
    public IEnumerable<Permission> Permissions { get; set; }
}
