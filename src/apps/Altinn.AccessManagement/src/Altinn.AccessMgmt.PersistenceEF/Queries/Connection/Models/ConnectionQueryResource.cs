namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

/// <summary>
/// Basic Resource refrence for Connection Query
/// </summary>
public sealed class ConnectionQueryResource
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Resource name
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
