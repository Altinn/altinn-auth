namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// PackageResource
/// </summary>
public class PackageResource
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

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
    public ExtResource Resource { get; set; }
}
