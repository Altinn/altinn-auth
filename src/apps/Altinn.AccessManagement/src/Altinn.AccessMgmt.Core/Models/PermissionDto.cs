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
    public CompactEntity From { get; set; }

    /// <summary>
    /// To party
    /// </summary>
    public CompactEntity To { get; set; }

    /// <summary>
    /// Via party
    /// </summary>
    public CompactEntity Via { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public CompactRole Role { get; set; }

    /// <summary>
    /// Via role
    /// </summary>
    public CompactRole ViaRole { get; set; }
}
