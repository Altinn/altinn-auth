namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Represents an area with relevant metadata and associated packages.
/// </summary>
public class AreaDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the area.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the area.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the unique resource name (URN) for the area.
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// Gets or sets the description of the area.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the icon representing the area.
    /// </summary>
    public string IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the list of packages available in this area.
    /// </summary>
    public List<PackageDto> Packages { get; set; }

    /// <summary>
    /// EntityGroup
    /// </summary>
    public AreaGroupDto Group { get; set; }
}
