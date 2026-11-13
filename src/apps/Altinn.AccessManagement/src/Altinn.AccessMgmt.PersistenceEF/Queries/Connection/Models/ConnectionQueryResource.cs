namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

/// <summary>
/// Basic Resource reference for Connection Query
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

    /// <summary>
    /// RefId
    /// </summary>
    public string RefId { get; init; } = string.Empty;

    /// <summary>
    /// Who last changed the assignment resource row, and when. Only set for resources assigned directly
    /// when <see cref="ConnectionQueryFilter.IncludeChangeInfo"/> is <c>true</c>.
    /// </summary>
    public ConnectionQueryChangeInfo ChangeInfo { get; init; }
}
