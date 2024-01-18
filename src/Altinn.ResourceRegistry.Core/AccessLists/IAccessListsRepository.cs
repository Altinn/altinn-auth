#nullable enable

using System.Collections.Immutable;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Repository for managing access lists.
/// </summary>
public interface IAccessListsRepository
{
    /// <summary>
    /// Create a new access list.
    /// </summary>
    /// <param name="registryOwner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="name">The registry name.</param>
    /// <param name="description">The registry description.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The newly created registry in the form of a <see cref="AccessListInfo"/>.</returns>
    public Task<AccessListInfo> CreateAccessList(
        string registryOwner,
        string identifier,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup access list by id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AccessListInfo"/>, if found, else <see langword="null"/></returns>
    public Task<AccessListInfo?> Lookup(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup access list by resource owner and identifier.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AccessListInfo"/>, if found, else <see langword="null"/></returns>
    public Task<AccessListInfo?> Lookup(string registryOwner, string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for an access list by it's id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListResourceConnection"/> for the given resource.</returns>
    public Task<IReadOnlyList<AccessListResourceConnection>> GetAccessListResourceConnections(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for an access list by it's resource owner and identifier.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListResourceConnection"/> for the given resource.</returns>
    public Task<IReadOnlyList<AccessListResourceConnection>> GetAccessListResourceConnections(
        string registryOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update access list with new values.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="newIdentifier">An optional new identifier, or <see langword="null"/> to keep the old value</param>
    /// <param name="newName">An optional new name, or <see langword="null"/> to keep the old value</param>
    /// <param name="newDescription">An optional new description, or <see langword="null"/> to keep the old value</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="AccessListInfo"/></returns>
    public Task<AccessListInfo> UpdateAccessList(
        Guid id,
        string? newIdentifier,
        string? newName,
        string? newDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update access list with new values.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="newIdentifier">An optional new identifier, or <see langword="null"/> to keep the old value</param>
    /// <param name="newName">An optional new name, or <see langword="null"/> to keep the old value</param>
    /// <param name="newDescription">An optional new description, or <see langword="null"/> to keep the old value</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="AccessListInfo"/></returns>
    public Task<AccessListInfo> UpdateAccessList(
        string registryOwner, 
        string identifier,
        string? newIdentifier,
        string? newName,
        string? newDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The deleted <see cref="AccessListInfo"/>.</returns>
    public Task<AccessListInfo> DeleteAccessList(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an access list.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The deleted <see cref="AccessListInfo"/>.</returns>
    public Task<AccessListInfo> DeleteAccessList(string registryOwner, string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a connection to a resource from an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> AddAccessListResourceConnection(
        Guid id, 
        string resourceIdentifier, 
        IEnumerable<string> actions, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a connection to a resource from an access list.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> AddAccessListResourceConnection(
        string registryOwner, 
        string identifier, 
        string resourceIdentifier, 
        IEnumerable<string> actions, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add additional actions to an access list resource connection.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> AddAccessListResourceConnectionActions(
        Guid id,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add additional actions to an access list resource connection.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> AddAccessListResourceConnectionActions(
        string registryOwner,
        string identifier,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove actions from an access list resource connection.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions to be removed from the allowed actions for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> RemoveAccessListResourceConnectionActions(
        Guid id,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove actions from an access list resource connection.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions to be removed from the allowed actions for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> RemoveAccessListResourceConnectionActions(
        string registryOwner,
        string identifier,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection to a resource from an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> DeleteAccessListResourceConnection(
        Guid id,
        string resourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection to a resource from an access list.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="AccessListResourceConnection"/> containing the information about the resource connection</returns>
    public Task<AccessListResourceConnection> DeleteAccessListResourceConnection(
        string registryOwner,
        string identifier,
        string resourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListMembership"/></returns>
    public Task<IReadOnlyList<AccessListMembership>> GetAccessListMemberships(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of an access list.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListMembership"/></returns>
    public Task<IReadOnlyList<AccessListMembership>> GetAccessListMemberships(
        string registryOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add members to an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="partyIds">The parties to add</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task AddAccessListMembers(
        Guid id,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add members to an access list.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="partyIds">The parties to add</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task AddAccessListMembers(
        string registryOwner,
        string identifier,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove members from an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="partyIds">The parties to remove</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task RemoveAccessListMembers(
        Guid id,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove members from an access list.
    /// </summary>
    /// <param name="registryOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="partyIds">The parties to remove</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task RemoveAccessListMembers(
        string registryOwner,
        string identifier,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);
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
