namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// For grouping of Areas
/// </summary>
public class AreaGroup
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// EntityTypeId
    /// </summary>
    public Guid EntityTypeId { get; set; }

    /// <summary>
    /// Urn
    /// </summary>
    public string Urn { get; set; }
}

/// <summary>
/// Extended AreaGroup
/// </summary>
public class ExtAreaGroup : AreaGroup
{
    /// <summary>
    /// EntityType
    /// </summary>
    public EntityType EntityType { get; set; }
}

/// <summary>
/// Extended AreaGroup
/// </summary>
public class ExtendedAreaGroup : AreaGroup
{
    /// <summary>
    /// EntityType
    /// </summary>
    public ExtendedEntityType EntityType { get; set; }
}
