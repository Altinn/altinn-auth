using Altinn.ResourceRegistry.Core.Aggregates;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Represents an access list aggregate.
/// </summary>
public interface IAccessListAggregate : IAggregate
{
    /// <summary>
    /// Gets the access list (optional) description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the access list identifier.
    /// </summary>
    string Identifier { get; }

    /// <summary>
    /// Gets the access list (display) name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the resource owner.
    /// </summary>
    string ResourceOwner { get; }

    /// <summary>
    /// Initialize a new access list.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier</param>
    /// <param name="name">The access list (display) name</param>
    /// <param name="description">The access list (optional) description</param>
    void Initialize(string resourceOwner, string identifier, string name, string description);

    /// <summary>
    /// Update the access list.
    /// </summary>
    /// <param name="identifier">The new identifier, or <see langword="null"/> to keep the old value</param>
    /// <param name="name">The new <see cref="Name"/>, or <see langword="null"/> to keep the old value</param>
    /// <param name="description">The new <see cref="Description"/>, or <see langword="null"/> to keep the old value</param>
    void Update(string identifier = null, string name = null, string description = null);

    /// <summary>
    /// Delete the access list.
    /// </summary>
    void Delete();

    /// <summary>
    /// Add a resource connection to the access list.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions allow-list</param>
    AccessListResourceConnection AddResourceConnection(string resourceIdentifier, IEnumerable<string> actions);

    /// <summary>
    /// Add actions to a resource connection.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions to add</param>
    AccessListResourceConnection AddResourceConnectionActions(string resourceIdentifier, IEnumerable<string> actions);

    /// <summary>
    /// Remove actions from a resource connection.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions to remove</param>
    AccessListResourceConnection RemoveResourceConnectionActions(string resourceIdentifier, IEnumerable<string> actions);

    /// <summary>
    /// Remove a resource connection from the access list.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    AccessListResourceConnection RemoveResourceConnection(string resourceIdentifier);

    /// <summary>
    /// Add members to the access list.
    /// </summary>
    /// <param name="partyIds">The members</param>
    void AddMembers(IEnumerable<Guid> partyIds);

    /// <summary>
    /// Remove members from the access list.
    /// </summary>
    /// <param name="partyIds">The members</param>
    void RemoveMembers(IEnumerable<Guid> partyIds);

    /// <summary>
    /// Gets the aggregate as a <see cref="AccessListInfo"/>.
    /// </summary>
    /// <param name="includes">What to include with the <see cref="AccessListInfo"/>.</param>
    /// <returns><see cref="AccessListInfo"/></returns>
    AccessListInfo AsAccessListInfo(AccessListIncludes includes = default);
}
