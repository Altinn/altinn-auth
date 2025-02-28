namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// EntityType
/// </summary>
public class EntityType
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
    /// Name
    /// </summary>
    public string Name { get; set; }
}

/// <summary>
/// Extended EntityType
/// </summary>
public class ExtEntityType : EntityType
{
    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }
}
