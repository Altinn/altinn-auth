namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

/// <summary>
/// Extended Connection with Packages and Resources
/// </summary>
public class ConnectionQueryExtendedRecord : ConnectionQueryRecord
{
    /// <summary>
    /// Packages
    /// </summary>
    public List<ConnectionQueryPackage> Packages { get; set; } = new();

    /// <summary>
    /// Resources
    /// </summary>
    public List<ConnectionQueryResource> Resources { get; set; } = new();

    /// <summary>
    /// Instances
    /// </summary>
    public List<ConnectionQueryInstance> Instances { get; set; } = new();

    /// <summary>
    /// Change info per package id for packages assigned directly on this record's assignment.
    /// Only set when <see cref="ConnectionQueryFilter.IncludeChangeInfo"/> is <c>true</c>.
    /// Kept on the record and not on <see cref="ConnectionQueryPackage"/> because package objects are shared between records.
    /// </summary>
    public IReadOnlyDictionary<Guid, ConnectionQueryChangeInfo> PackageChangeInfo { get; set; }
}
