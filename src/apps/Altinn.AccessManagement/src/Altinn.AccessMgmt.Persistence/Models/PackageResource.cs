using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// PackageResource
/// </summary>
public class PackageResource
{
    private Guid _id;

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
    public Guid Id
    {
        get => _id;
        set
        {
            if (!value.IsVersion7Uuid())
            {
                throw new ArgumentException("Id must be a version 7 UUID", nameof(value));
            }

            _id = value;
        }
    }

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

/// <summary>
/// Extended PackageResource
/// </summary>
public class ExtendedPackageResource : PackageResource
{
    /// <summary>
    /// Package
    /// </summary>
    public ExtendedPackage Package { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public ExtendedResource Resource { get; set; }
}
