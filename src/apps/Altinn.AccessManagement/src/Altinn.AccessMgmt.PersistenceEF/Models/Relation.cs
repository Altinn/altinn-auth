using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// New Connection
/// </summary>
public class Relation : BaseRelation
{
    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public CompactEntity From { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public CompactRole Role { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public CompactEntity Via { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public CompactRole ViaRole { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public CompactEntity To { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public CompactPackage Package { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public CompactResource Resource { get; set; }
}
