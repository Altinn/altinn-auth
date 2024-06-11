#nullable enable

using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Core.Register;

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
    /// <param name="resourceIdentifier">
    /// Optional resource identifier. Used if <paramref name="includes"/> contains flag <see cref="AccessListIncludes.ResourceConnections"/>
    /// to filter the resource connections included.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Page{TItem, TToken}"/> of <see cref="AccessListInfo"/>.</returns>
    Task<Page<AccessListInfo, string>> GetAccessListsByOwner(
        string owner,
        Page<string>.Request request,
        AccessListIncludes includes = default,
        string? resourceIdentifier = null,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Gets a page of access list resource-connections by owner and identifier.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="request">The page request.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A conditional page of <see cref="AccessListResourceConnection"/>.</returns>
    Task<Conditional<VersionedPage<AccessListResourceConnection, string, ulong>, ulong>> GetAccessListResourceConnections(
        string owner,
        string identifier,
        Page<string>.Request request,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates an access list resource-connection.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="resourceIdentifier">The resource identifier.</param>
    /// <param name="actions">What actions to allow on the resource.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A conditional <see cref="AccessListResourceConnection"/>.</returns>
    Task<Conditional<AccessListData<AccessListResourceConnection>, ulong>> UpsertAccessListResourceConnection(
        string owner,
        string identifier,
        string resourceIdentifier,
        IReadOnlyList<string> actions,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an access list resource-connection.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="resourceIdentifier">The resource identifier.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A conditional <see cref="AccessListResourceConnection"/>.</returns>
    Task<Conditional<AccessListData<AccessListResourceConnection>, ulong>> DeleteAccessListResourceConnection(
        string owner,
        string identifier,
        string resourceIdentifier,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a page of access list members by owner and identifier.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="request">The page request.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> GetAccessListMembers(
        string owner,
        string identifier,
        Page<Guid?>.Request request,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the access list members.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="parties">The new parties.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// The result of calling <see cref="GetAccessListMembers(string, string, Page{Guid?}.Request, IVersionedEntityCondition{ulong}?, CancellationToken)"/>
    /// after modifying the access list.
    /// </returns>
    Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> ReplaceAccessListMembers(
        string owner,
        string identifier,
        IReadOnlyList<PartyUrn> parties,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new members to the access list.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="parties">The new parties.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// The result of calling <see cref="GetAccessListMembers(string, string, Page{Guid?}.Request, IVersionedEntityCondition{ulong}?, CancellationToken)"/>
    /// after modifying the access list.
    /// </returns>
    Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> AddAccessListMembers(
        string owner,
        string identifier,
        IReadOnlyList<PartyUrn> parties,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove members to the access list.
    /// </summary>
    /// <param name="owner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="parties">The new parties.</param>
    /// <param name="condition">Optional condition on the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// The result of calling <see cref="GetAccessListMembers(string, string, Page{Guid?}.Request, IVersionedEntityCondition{ulong}?, CancellationToken)"/>
    /// after modifying the access list.
    /// </returns>
    Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> RemoveAccessListMembers(
        string owner,
        string identifier,
        IReadOnlyList<PartyUrn> parties,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the access list memberships for a party.
    /// </summary>
    /// <param name="partyUrns">The party urns.</param>
    /// <param name="resourceUrns">The resource urns.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>All resource connections to access-lists for the given resource which the provided party is a member off.</returns>
    Task<Result<IReadOnlyCollection<KeyValuePair<AccessListResourceConnection, AccessListMembership>>>> GetMembershipsForPartiesAndResources(
        IEnumerable<PartyUrn>? partyUrns,
        IEnumerable<ResourceUrn>? resourceUrns,
        CancellationToken cancellationToken);
}
