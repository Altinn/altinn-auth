#nullable enable

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Repository for managing access lists.
/// </summary>
public interface IAccessListsRepository
{
    /// <summary>
    /// Gets access lists by owner, limited by <paramref name="count"/> and optionally starting from <paramref name="continueFrom"/>.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="continueFrom">An optional value to continue iterating from. This value is an <see cref="AccessListInfo.Identifier"/> to start from, using greater than or equals comparison.</param>
    /// <param name="count">The total number of entries to return.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListInfo"/>, sorted by <see cref="AccessListInfo.Identifier"/> and limited by <paramref name="count"/>.</returns>
    Task<IReadOnlyList<AccessListInfo>> GetAccessListsByOwner(string resourceOwner, string? continueFrom, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup access list info by id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AccessListInfo"/>, if found, else <see langword="null"/></returns>
    Task<AccessListInfo?> LookupInfo(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup access list info by resource owner and identifier.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AccessListInfo"/>, if found, else <see langword="null"/></returns>
    Task<AccessListInfo?> LookupInfo(string resourceOwner, string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for an access list by it's id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListResourceConnection"/> for the given resource.</returns>
    Task<IReadOnlyList<AccessListResourceConnection>?> GetAccessListResourceConnections(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for an access list by it's resource owner and identifier.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListResourceConnection"/> for the given resource.</returns>
    Task<IReadOnlyList<AccessListResourceConnection>?> GetAccessListResourceConnections(
        string resourceOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListMembership"/></returns>
    Task<IReadOnlyList<AccessListMembership>?> GetAccessListMemberships(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of an access list.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListMembership"/></returns>
    Task<IReadOnlyList<AccessListMembership>?> GetAccessListMemberships(
        string resourceOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new access list.
    /// </summary>
    /// <param name="resourceOwner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="name">The registry name.</param>
    /// <param name="description">The registry description.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The newly created registry in the form of a <see cref="AccessListInfo"/>.</returns>
    Task<IAccessListAggregate> CreateAccessList(
        string resourceOwner,
        string identifier,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load an access list aggregate.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IAccessListAggregate"/>, if found, else <see langword="null"/></returns>
    Task<IAccessListAggregate?> LoadAccessList(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load an access list aggregate.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IAccessListAggregate"/>, if found, else <see langword="null"/></returns>
    Task<IAccessListAggregate?> LoadAccessList(string resourceOwner, string identifier, CancellationToken cancellationToken = default);
}
