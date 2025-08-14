using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Package
/// </summary>
public class Package : BasePackage
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
