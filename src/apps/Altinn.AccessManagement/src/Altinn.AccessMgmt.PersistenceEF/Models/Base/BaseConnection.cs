using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// New Connection
/// </summary>
[NotMapped]
public class BaseConnection
{
    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public Guid? ViaId { get; set; }

    /// <summary>
    /// The role the facilitator has to the client
    /// </summary>
    public Guid? ViaRoleId { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public Guid ToId { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Guid? PackageId { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// DelegationId
    /// </summary>
    public Guid? DelegationId { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; }
}
