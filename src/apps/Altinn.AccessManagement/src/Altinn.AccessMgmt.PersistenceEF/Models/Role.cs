using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Role
/// </summary>
public class Role : BaseRole
{
    /// <summary>
    /// EntityType
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }
}
