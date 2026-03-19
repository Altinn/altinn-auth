namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

/// <summary>
/// Basic Instance reference for Connection Query
/// </summary>
public sealed class ConnectionQueryInstance
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public Guid ResourceId { get; init; }

    /// <summary>
    /// Instance identifier (e.g., "51599233/df333e75-5896-4254-a69f-146736eaf668")
    /// </summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the resource associated with this instance.
    /// </summary>
    public string ResourceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the reference ID of the resource associated with this instance.
    /// </summary>
    public string ResourceRefId { get; init; } = string.Empty;
}
