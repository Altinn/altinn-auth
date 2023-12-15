using System.Collections.Immutable;

namespace Altinn.ResourceRegistry.Core.PartyRegistry;

/// <summary>
/// Repository for managing party registries.
/// </summary>
public interface IPartyRegistryRepository
{
    /// <summary>
    /// Create a new party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner (org.nr.).</param>
    /// <param name="identifier">The registry identifier (unique per owner).</param>
    /// <param name="name">The registry name.</param>
    /// <param name="description">The registry description.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The newly created registry in the form of a <see cref="PartyRegistryInfo"/>.</returns>
    public Task<PartyRegistryInfo> CreatePartyRegistry(
        string registryOwner,
        string identifier,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup party registry by registry id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="PartyRegistryInfo"/>, if found, else <see langword="null"/></returns>
    public Task<PartyRegistryInfo?> Lookup(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup party registry by registry registry owner and identifier.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="PartyRegistryInfo"/>, if found, else <see langword="null"/></returns>
    public Task<PartyRegistryInfo?> Lookup(string registryOwner, string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for a party registry by it's id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="PartyRegistryResourceConnection"/> for the given resource.</returns>
    public Task<IReadOnlyList<PartyRegistryResourceConnection>> GetResourceConnections(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for a party registry by it's registry owner and identifier.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="PartyRegistryResourceConnection"/> for the given resource.</returns>
    public Task<IReadOnlyList<PartyRegistryResourceConnection>> GetResourceConnections(
        string registryOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update party registry with new values.
    /// </summary>
    /// <param name="id">The registry id</param>
    /// <param name="newIdentifier">An optional new identifier, or <see langword="nul"/> to keep the old value</param>
    /// <param name="newName">An optional new name, or <see langword="nul"/> to keep the old value</param>
    /// <param name="newDescription">An optional new description, or <see langword="nul"/> to keep the old value</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="PartyRegistryInfo"/></returns>
    public Task<PartyRegistryInfo> UpdatePartyRegistry(
        Guid id,
        string? newIdentifier,
        string? newName,
        string? newDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update party registry with new values.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="newIdentifier">An optional new identifier, or <see langword="nul"/> to keep the old value</param>
    /// <param name="newName">An optional new name, or <see langword="nul"/> to keep the old value</param>
    /// <param name="newDescription">An optional new description, or <see langword="nul"/> to keep the old value</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="PartyRegistryInfo"/></returns>
    public Task<PartyRegistryInfo> UpdatePartyRegistry(
        string registryOwner, 
        string identifier,
        string? newIdentifier,
        string? newName,
        string? newDescription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a party registry.
    /// </summary>
    /// <param name="id">The registry id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The deleted <see cref="PartyRegistryInfo"/>.</returns>
    public Task<PartyRegistryInfo> DeletePartyRegistry(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The deleted <see cref="PartyRegistryInfo"/>.</returns>
    public Task<PartyRegistryInfo> DeletePartyRegistry(string registryOwner, string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a connection to a resource from a party registry.
    /// </summary>
    /// <param name="id">The party registry id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnection(
        Guid id, 
        string resourceIdentifier, 
        IEnumerable<string> actions, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a connection to a resource from a party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnection(
        string registryOwner, 
        string identifier, 
        string resourceIdentifier, 
        IEnumerable<string> actions, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add additional actions to a party registry resource connection.
    /// </summary>
    /// <param name="id">The party registry id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnectionActions(
        Guid id,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add additional actions to a party registry resource connection.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions allowed for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnectionActions(
        string registryOwner,
        string identifier,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove actions to a party registry resource connection.
    /// </summary>
    /// <param name="id">The party registry id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions to be removed from the allowed actions for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> RemovePartyResourceConnectionActions(
        Guid id,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove actions to a party registry resource connection.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The set of actions to be removed from the allowed actions for the members of the registry on the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> RemovePartyResourceConnectionActions(
        string registryOwner,
        string identifier,
        string resourceIdentifier,
        IEnumerable<string> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection to a resource from a party registry.
    /// </summary>
    /// <param name="id">The party registry id</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> DeletePartyResourceConnection(
        Guid id,
        string resourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a connection to a resource from a party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="PartyRegistryResourceConnection"/> containing the information about the resource connection</returns>
    public Task<PartyRegistryResourceConnection> DeletePartyResourceConnection(
        string registryOwner,
        string identifier,
        string resourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of a party registry.
    /// </summary>
    /// <param name="id">The party registry id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="PartyRegistryMembership"/></returns>
    public Task<IReadOnlyList<PartyRegistryMembership>> GetPartyRegistryMemberships(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of a party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="PartyRegistryMembership"/></returns>
    public Task<IReadOnlyList<PartyRegistryMembership>> GetPartyRegistryMemberships(
        string registryOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add members to a party registry.
    /// </summary>
    /// <param name="id">The party registry id</param>
    /// <param name="partyIds">The parties to add</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task AddPartyRegistryMembers(
        Guid id,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add members to a party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="partyIds">The parties to add</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task AddPartyRegistryMembers(
        string registryOwner,
        string identifier,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove members from a party registry.
    /// </summary>
    /// <param name="id">The party registry id</param>
    /// <param name="partyIds">The parties to remove</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task RemovePartyRegistryMembers(
        Guid id,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove members from a party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="partyIds">The parties to remove</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task RemovePartyRegistryMembers(
        string registryOwner,
        string identifier,
        IEnumerable<Guid> partyIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a party registry.
/// </summary>
/// <param name="Id">The database id for the party registry.</param>
/// <param name="RegistryOwner">The registry owner (a org.nr.).</param>
/// <param name="Identifier">The registry owner-unique identifier. Limited to 'a'-'z' and '-' characters.</param>
/// <param name="Name">The registry name. Does not have to be unique, and can contain any characters.</param>
/// <param name="Description">A registry description.</param>
/// <param name="CreatedAt">When this registry was created.</param>
/// <param name="UpdatedAt">When this registry was last updated.</param>
public record PartyRegistryInfo(
    Guid Id,
    string RegistryOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Information about a party registry resource connection.
/// </summary>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The allowed actions.</param>
/// <param name="Created">When the connection was created.</param>
/// <param name="Modified">When the connection was last modified.</param>
public record PartyRegistryResourceConnection(
    string ResourceIdentifier,
    ImmutableHashSet<string> Actions,
    DateTimeOffset Created,
    DateTimeOffset Modified);

/// <summary>
/// Information about a party registry membership.
/// </summary>
/// <param name="PartyId">The party id.</param>
/// <param name="Since">When the party was added to the registry.</param>
public record PartyRegistryMembership(
    Guid PartyId,
    DateTimeOffset Since);