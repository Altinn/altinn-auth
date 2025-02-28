namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Resource
/// </summary>
public class Resource
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

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
