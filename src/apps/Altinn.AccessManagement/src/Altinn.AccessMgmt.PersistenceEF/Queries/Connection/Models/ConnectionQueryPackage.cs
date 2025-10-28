namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

/// <summary>
/// Basic Package refrence for Connection Query
/// </summary>
public sealed class ConnectionQueryPackage
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Package name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Package resources
    /// </summary>
    public List<ConnectionQueryResource> Resources { get; set; }
}
