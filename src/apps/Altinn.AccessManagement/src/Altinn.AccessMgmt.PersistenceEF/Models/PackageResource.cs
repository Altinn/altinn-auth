using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended PackageResource
/// </summary>
public class PackageResource : BasePackageResource
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
