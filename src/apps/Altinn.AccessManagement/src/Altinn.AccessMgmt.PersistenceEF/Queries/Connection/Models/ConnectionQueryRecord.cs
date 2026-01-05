using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// Represents a record of a connection query, detailing the entities involved and their roles.
/// </summary>
public class ConnectionQueryRecord : ConnectionQueryBaseRecord
{
    /// <summary>
    /// Connection source entity
    /// </summary>
    public Entity From { get; set; } = null!;

    /// <summary>
    /// Connection destination entity
    /// </summary>
    public Entity To { get; set; } = null!;

    /// <summary>
    /// Connection role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Connection Delegation Via
    /// </summary>
    public Entity Via { get; set; }

    /// <summary>
    /// Connection Delegation Via Role
    /// </summary>
    public Role ViaRole { get; set; }
}
