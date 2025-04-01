namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Resource
/// </summary>
public class Resource
{
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
    public Guid Id { get; private set; }

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
