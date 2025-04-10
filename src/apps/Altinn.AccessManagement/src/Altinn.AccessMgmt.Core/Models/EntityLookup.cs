using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Lookup for entity
/// </summary>
public class EntityLookup
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityLookup"/> class.
    /// </summary>
    public EntityLookup()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id
    {
        get => _id;
        set
        {
            if (!value.IsVersion7Uuid())
            {
                throw new ArgumentException("Id must be a version 7 UUID", nameof(value));
            }

            _id = value;
        }
    }

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
