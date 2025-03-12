namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Lookup values for role
/// </summary>
public class RoleLookup
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role identifier
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Key (e.g Party,SSN,OrgNo)
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; }
}

/// <summary>
/// Extended role lookup
/// </summary>
public class ExtRoleLookup : RoleLookup
{
    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }
}
