using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Extended Base DbAudit
/// </summary>
public class ExtDbAudit : DbAudit
{
    /// <summary>
    /// Entity responsible for latest version
    /// </summary>
    public Entity ChangedByEntity { get; set; }

    /// <summary>
    /// The system entity used to create latest version
    /// </summary>
    public Entity ChangedViaEntity { get; set; }

    /// <summary>
    /// Entity responsible for deleting record
    /// </summary>
    public Entity DeletedByEntity { get; set; }

    /// <summary>
    /// System used for deleting record
    /// </summary>
    public Entity DeletedViaEntity { get; set; }
}

/// <summary>
/// Extended Base DbAudit
/// </summary>
public class ExtTypedDbAudit<T> : TypedDbAudit<T>
{
    /// <summary>
    /// Entity responsible for latest version
    /// </summary>
    public Entity ChangedByEntity { get; set; }

    /// <summary>
    /// The system entity used to create latest version
    /// </summary>
    public Entity ChangedViaEntity { get; set; }

    /// <summary>
    /// Entity responsible for deleting record
    /// </summary>
    public Entity DeletedByEntity { get; set; }

    /// <summary>
    /// System used for deleting record
    /// </summary>
    public Entity DeletedViaEntity { get; set; }
}
