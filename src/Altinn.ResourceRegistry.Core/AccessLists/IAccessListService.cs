#nullable enable

using System.Collections.Immutable;
using Altinn.ResourceRegistry.Core.Models;

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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Page{TItem, TToken}"/> of <see cref="AccessListInfo"/>.</returns>
    Task<Page<AccessListInfo, string>> GetAccessListsByOwner(string owner, Page<string>.Request request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an access list.
/// </summary>
/// <param name="Id">The database id for the access list.</param>
/// <param name="RegistryOwner">The resource owner (a org.nr.).</param>
/// <param name="Identifier">The resource owner-unique identifier. Limited to 'a'-'z' and '-' characters.</param>
/// <param name="Name">The registry name. Does not have to be unique, and can contain any characters.</param>
/// <param name="Description">A registry description.</param>
/// <param name="CreatedAt">When this registry was created.</param>
/// <param name="UpdatedAt">When this registry was last updated.</param>
public record AccessListInfo(
    Guid Id,
    string RegistryOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Information about an access list resource connection.
/// </summary>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The allowed actions.</param>
/// <param name="Created">When the connection was created.</param>
/// <param name="Modified">When the connection was last modified.</param>
public record AccessListResourceConnection(
    string ResourceIdentifier,
    ImmutableHashSet<string> Actions,
    DateTimeOffset Created,
    DateTimeOffset Modified);

/// <summary>
/// Information about an access list membership.
/// </summary>
/// <param name="PartyId">The party id.</param>
/// <param name="Since">When the party was added to the registry.</param>
public record AccessListMembership(
    Guid PartyId,
    DateTimeOffset Since);
