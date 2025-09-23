namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Represents a group of areas, categorized under a specific entity type.
/// </summary>
public class AreaGroupDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the area group.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the area group.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the unique resource name (URN) for the area group.
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// Gets or sets the description of the area group.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the type of entity this area group represents.
    /// </summary>
    public string Type { get; set; } // EntityType

    /// <summary>
    /// Gets or sets the list of areas that belong to this group.
    /// </summary>
    public List<AreaDto> Areas { get; set; }
}
