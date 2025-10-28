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
}
