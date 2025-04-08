namespace Altinn.AccessMgmt.Persistence.Core.Models;

/// <summary>
/// Holds audit information
/// </summary>
public class DbAudit
{
    /// <summary>
    /// When this version is valid from
    /// </summary>
    public DateTimeOffset ValidFrom { get; set; }

    /// <summary>
    /// When this version is valid to (only used in history schema)
    /// </summary>
    public DateTimeOffset ValidTo { get; set; }

    /// <summary>
    /// Identify change operation spanning multiple tables and cascades
    /// </summary>
    public Guid ChangeOperation { get; set; }

    /// <summary>
    /// User responsible for latest version
    /// </summary>
    public Guid ChangedBy { get; set; }

    /// <summary>
    /// The system used to create latest version
    /// </summary>
    public Guid ChangedBySystem { get; set; }

    /// <summary>
    /// User responsible for deleting record (only used in history schema)
    /// </summary>
    public Guid DeletedBy { get; set; }

    /// <summary>
    /// System used for deleting record (only used in history schema)
    /// </summary>
    public Guid DeletedBySystem { get; set; }

    /// <summary>
    /// Identify delete operation spanning multiple tables and cascades
    /// </summary>
    public Guid DeleteOperation { get; set; }
}

/// <summary>
/// Extended DbAudit with Object
/// </summary>
/// <typeparam name="T"></typeparam>
public class TypedDbAudit<T> : DbAudit
{
    /// <summary>
    /// Object
    /// </summary>
    public T Object { get; set; }
}

/// <summary>
/// Base interface for extending classes with audit information
/// </summary>
public interface IBaseAudit
{
    /// <summary>
    /// Holds audit information
    /// </summary>
    public DbAudit Audit { get; set; }
}
