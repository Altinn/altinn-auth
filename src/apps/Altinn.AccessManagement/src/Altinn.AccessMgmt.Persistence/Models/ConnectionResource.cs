namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// Packages available on connections
/// </summary>
public class ConnectionResource
{
    /// <summary>
    /// Identifier, AssignmentId or DelegationId
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public Guid ResourceId { get; set; }
}

/// <summary>
/// Extended Connection Resources
/// </summary>
public class ExtConnectionResource : ConnectionResource
{
    /// <summary>
    /// Connection => Assignment or Delegation
    /// </summary>
    public Connection Connection { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}
