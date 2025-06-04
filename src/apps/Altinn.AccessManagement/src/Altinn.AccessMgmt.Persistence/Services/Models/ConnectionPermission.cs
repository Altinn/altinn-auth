using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Connection Permission
/// </summary>
public class ConnectionPermission
{
    /// <summary>
    /// The party with permission
    /// </summary>
    public CompactEntity Party { get; set; }

    /// <summary>
    /// KeyRole permissions
    /// </summary>
    public List<Permission> KeyRoles { get; set; }

    /// <summary>
    /// Delegated permissions
    /// </summary>
    public List<Permission> Delegations { get; set; }
}
