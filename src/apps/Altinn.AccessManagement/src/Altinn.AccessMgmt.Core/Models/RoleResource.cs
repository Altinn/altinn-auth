namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Resources mapped directly to roles
/// </summary>
public class RoleResource
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role identity
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Resource identity
    /// </summary>
    public Guid ResourceId { get; set; }
}

/// <summary>
/// Extended Role Resource
/// </summary>
public class ExtRoleResource : RoleResource
{
    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}
