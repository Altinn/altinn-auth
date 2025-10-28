namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

/// <summary>
/// Base row for ConnectionQuery
/// </summary>
public class ConnectionQueryBaseRecord
{
    /// <summary>
    /// Connection source entity identity
    /// </summary>
    public Guid FromId { get; init; }

    /// <summary>
    /// Connection destination entity identity
    /// </summary>
    public Guid ToId { get; init; }

    /// <summary>
    /// Connection role identity
    /// </summary>
    public Guid RoleId { get; init; }
    
    /// <summary>
    /// Connection Assignment Identity
    /// </summary>
    public Guid? AssignmentId { get; init; }

    /// <summary>
    /// Connection Delegation Identity
    /// </summary>
    public Guid? DelegationId { get; init; }

    /// <summary>
    /// Connection Delegation Via Identity
    /// </summary>
    public Guid? ViaId { get; init; }

    /// <summary>
    /// Connection Delegation Via Role Identity
    /// </summary>
    public Guid? ViaRoleId { get; init; }

    /// <summary>
    /// Connection Composite Key
    /// </summary>
    public ConnectionCompositeKey CompositeKey() => new(FromId, ToId, RoleId, AssignmentId, DelegationId, ViaId, ViaRoleId);
}

/// <summary>
/// A Composite key for Connections
/// </summary>
public readonly record struct ConnectionCompositeKey(
    Guid FromId,
    Guid ToId,
    Guid RoleId,
    Guid? AssignmentId,
    Guid? DelegationId,
    Guid? ViaId,
    Guid? ViaRoleId
)
{ }
