namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Packages available on connections
/// </summary>
public class ConnectionPackage
{
    /// <summary>
    /// Identifier, AssignmentId or DelegationId
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Package identifier
    /// </summary>
    public Guid PackageId { get; set; }

    /*
    public bool HasAccess { get; set; }
    public bool CanDelegate { get; set; }
    */
}

/// <summary>
/// Extended connection packages
/// </summary>
public class ExtConnectionPackage : ConnectionPackage
{
    /// <summary>
    /// Connection
    /// </summary>
    public Connection Connection { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }
}
