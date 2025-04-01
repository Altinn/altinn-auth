namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// RoleMap
/// Entities with a one roile can also get another one
/// </summary>
public class RoleMap
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleMap"/> class.
    /// </summary>
    public RoleMap()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// HasRoleId
    /// </summary>
    public Guid HasRoleId { get; set; }

    /// <summary>
    /// GetRoleId
    /// </summary>
    public Guid GetRoleId { get; set; }
}

/// <summary>
/// Extended RoleMap
/// </summary>
public class ExtRoleMap : RoleMap
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
