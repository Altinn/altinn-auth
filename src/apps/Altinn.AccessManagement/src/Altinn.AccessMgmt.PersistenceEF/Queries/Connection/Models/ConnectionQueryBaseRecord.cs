using Altinn.Authorization.Api.Contracts.AccessManagement;

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
    /// Indicates if the connection is through key role access
    /// </summary>
    public bool IsKeyRoleAccess { get; set; }

    /// <summary>
    /// Indicates if the connection is through access for the main unit
    /// </summary>
    public bool IsMainUnitAccess { get; set; }

    /// <summary>
    /// Indicates if the connection is through another role (RoleMap)
    /// </summary>
    public bool IsRoleMap { get; set; }

    /// <summary>
    /// Reason for connection
    /// </summary>
    public ConnectionReason Reason { get; set; }

    /// <summary>
    /// Connection Composite Key
    /// </summary>
    public ConnectionCompositeKey CompositeKey() => new(FromId, ToId, RoleId, AssignmentId, DelegationId, ViaId, ViaRoleId);
}

/// <summary>
/// Describes the reason for a connection
/// </summary>
public enum ConnectionReason
{
    /// <summary>
    /// Connection originates from an Assignment
    /// </summary>
    Assignment,

    /// <summary>
    /// Connection originates from a Delegation
    /// </summary>
    Delegation,

    /// <summary>
    /// Connection originates from a parent/child hierarchy
    /// </summary>
    Hierarchy,

    /// <summary>
    /// Connection originates from a RoleMap
    /// </summary>
    RoleMap,

    /// <summary>
    /// Connection originates from a key role (Role.IsKeyRole)
    /// </summary>
    KeyRole
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

public sealed class ConnectionCoreComparer : IEqualityComparer<ConnectionQueryBaseRecord>
{
    public bool Equals(ConnectionQueryBaseRecord? x, ConnectionQueryBaseRecord? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.FromId == y.FromId
            && x.ToId == y.ToId
            && x.RoleId == y.RoleId
            && Nullable.Equals(x.ViaId, y.ViaId)
            && Nullable.Equals(x.ViaRoleId, y.ViaRoleId);
    }

    public int GetHashCode(ConnectionQueryBaseRecord? obj)
    {
        if (obj is null)
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + obj.FromId.GetHashCode();
            hash = (hash * 23) + obj.ToId.GetHashCode();
            hash = (hash * 23) + obj.RoleId.GetHashCode();
            hash = (hash * 23) + (obj.ViaId?.GetHashCode() ?? 0);
            hash = (hash * 23) + (obj.ViaRoleId?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

public static class ConnectionDiffHelper
{
    private static readonly ConnectionCoreComparer Comparer = new();

    public static (
        List<ConnectionQueryBaseRecord> OnlyInA, 
        List<ConnectionQueryBaseRecord> OnlyInB
        ) 
            Diff(
                IEnumerable<ConnectionQueryBaseRecord> a, 
                IEnumerable<ConnectionQueryBaseRecord> b
            )
    {
        var onlyA = a.Except(b, Comparer).ToList();
        var onlyB = b.Except(a, Comparer).ToList();
        return (onlyA, onlyB);
    }
}
