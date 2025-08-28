using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// New Connection
/// </summary>
public class Connection : BaseConnection
{
    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public Entity From { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public Entity Via { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public Role ViaRole { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public Entity To { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}
