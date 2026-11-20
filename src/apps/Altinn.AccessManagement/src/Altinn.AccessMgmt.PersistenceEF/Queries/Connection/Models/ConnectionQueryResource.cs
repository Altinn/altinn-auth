using Altinn.Authorization.Api.Contracts.ResourceRegistry.Enums;

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
    /// Whether the resource can be delegated
    /// </summary>
    public bool Delegable { get; init; }

    /// <summary>
    /// Access list mode of the resource
    /// </summary>
    public ResourceAccessListMode AccessListMode { get; init; }
}
