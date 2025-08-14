using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Connection
/// </summary>
public class Connection : BaseConnection
{
    /// <summary>
    /// The entity the connection is from (origin, client, source etc)
    /// For Assignments this is From for Delegations this is From.From
    /// </summary>
    public Entity From { get; set; }

    /// <summary>
    /// The role To identifies as either to From or to Facilitator
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// The entity the connection is to (destination, agent, etc)
    /// For Assignments this is To for Delegations this is To.To
    /// </summary>
    public Entity To { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public Entity Via { get; set; }

    /// <summary>
    /// The role the facilitator has to the client 
    /// </summary>
    public Role ViaRole { get; set; }
}
