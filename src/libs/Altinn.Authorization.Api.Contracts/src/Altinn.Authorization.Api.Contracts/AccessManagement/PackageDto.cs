namespace Altinn.Authorization.Api.Contracts.AccessManagement;

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
    /// Indicates if the package can be used for delegation
    /// </summary>
    public bool IsDelegable { get; set; }

    /// <summary>
    /// Indicates if the package can be used for delegation
    /// </summary>
    public bool IsAssignable { get; set; }

    /// <summary>
    /// Indicates if the package can be used as subject for authorization in resource policy
    /// </summary>
    public bool IsResourcePolicyAvailable { get; set; }

    /// <summary>
    /// Gets or sets the area associated with the package.
    /// </summary>
    public AreaDto Area { get; set; }

    /// <summary>
    /// The type of party the package is intended for
    /// </summary>
    public TypeDto Type { get; set; }

    /// <summary>
    /// Gets or sets the collection of resources linked to the package.
    /// </summary>
    public IEnumerable<ResourceDto> Resources { get; set; }
}
