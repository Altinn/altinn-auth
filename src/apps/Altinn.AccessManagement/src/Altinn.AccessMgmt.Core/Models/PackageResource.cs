namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// PackageResource
/// </summary>
public class PackageResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackageResource"/> class.
    /// </summary>
    public PackageResource()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// PackageId
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// ResourceId
    /// </summary>
    public Guid ResourceId { get; set; }
}

/// <summary>
/// Extended PackageResource
/// </summary>
public class ExtPackageResource : PackageResource
{
    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}
