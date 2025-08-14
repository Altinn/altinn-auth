using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// Resource
/// </summary>
public class Resource
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="Resource"/> class.
    /// </summary>
    public Resource()
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
    /// ProviderId
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// TypeId
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Refrence identifier
    /// </summary>
    public string RefId { get; set; }
}

/// <summary>
/// Extended Resource
/// </summary>
public class ExtResource : Resource
{
    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public ResourceType Type { get; set; }
}

/// <summary>
/// Extended Resource
/// </summary>
public class ExtendedResource : Resource
{
    /// <summary>
    /// Provider
    /// </summary>
    public ExtendedProvider Provider { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public ExtendedResourceType Type { get; set; }
}

/// <summary>
/// Compact versjon of resource
/// </summary>
public class CompactResource
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; }
}
