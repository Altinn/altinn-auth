#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Service for dealing with access lists.
/// </summary>
public interface IAccessListService
{
    /// <summary>
    /// Gets a page of access lists by owner.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="request">The page request.</param>
    /// <param name="includes">What additional to include in the response.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Page{TItem, TToken}"/> of <see cref="AccessListInfo"/>.</returns>
    Task<Page<AccessListInfo, string>> GetAccessListsByOwner(string owner, Page<string>.Request request, AccessListIncludes includes = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an access list by owner and identifier.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="includes">What additional to include in the response.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A conditional <see cref="AccessListInfo"/></returns>
    Task<Conditional<AccessListInfo, ulong>> GetAccessList(
        string owner,
        string identifier,
        AccessListIncludes includes = default,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an access list by owner and identifier.
    /// </summary>
    /// <remarks>Returns <see cref="Conditional{T, TTag}.IsNotFound"/> if the access list is already deleted.</remarks>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A conditional <see cref="AccessListInfo"/></returns>
    Task<Conditional<AccessListInfo, ulong>> DeleteAccessList(
        string owner,
        string identifier,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates an access list.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="name">The access list name.</param>
    /// <param name="description">The access list description.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A conditional <see cref="AccessListInfo"/></returns>
    Task<Conditional<AccessListInfo, ulong>> CreateOrUpdateAccessList(
        string owner,
        string identifier,
        string name,
        string description,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);
}
