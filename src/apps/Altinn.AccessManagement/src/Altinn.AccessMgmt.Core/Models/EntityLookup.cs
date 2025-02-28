namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Lookup for entity
/// </summary>
public class EntityLookup
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Entity
    /// </summary>
    public Guid EntityId { get; set; }

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
/// Extended Entity Lookup
/// </summary>
public class ExtEntityLookup : EntityLookup
{
    /// <summary>
    /// Entity
    /// </summary>
    public Entity Entity { get; set; }
}
