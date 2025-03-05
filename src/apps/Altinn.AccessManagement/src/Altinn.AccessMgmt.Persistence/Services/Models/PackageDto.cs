using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Represents a package with related metadata and associated resources.
/// </summary>
public class PackageDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the package.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the package.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the unique resource name (URN) for the package.
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// Gets or sets the description of the package.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the area associated with the package.
    /// </summary>
    public ExtArea Area { get; set; }

    /// <summary>
    /// Gets or sets the collection of resources linked to the package.
    /// </summary>
    public IEnumerable<Resource> Resources { get; set; }
}
