namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Package
/// </summary>
public class Package
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
    /// EntityTypeId
    /// </summary>
    public Guid EntityTypeId { get; set; }

    /// <summary>
    /// AreaId
    /// </summary>
    public Guid AreaId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Can be assigned
    /// </summary>
    public bool IsAssignable { get; set; }

    /// <summary>
    /// Can be delegate
    /// </summary>
    public bool IsDelegable { get; set; }

    /// <summary>
    /// Has resources
    /// </summary>
    public bool HasResources { get; set; }

    /// <summary>
    /// Urn
    /// </summary>
    public string Urn { get; set; }
}

/// <summary>
/// Extended Package
/// </summary>
public class ExtPackage : Package
{
    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }

    /// <summary>
    /// EntityType
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// Area
    /// </summary>
    public Area Area { get; set; }
}
