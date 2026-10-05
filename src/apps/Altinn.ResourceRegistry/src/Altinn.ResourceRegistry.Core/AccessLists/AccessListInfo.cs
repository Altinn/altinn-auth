#nullable enable

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Information about an access list.
/// </summary>
/// <param name="Id"><inheritdoc cref="AccessListMetadata(Guid, DateTimeOffset, ulong)" path="/param[@name='Id']/node()"/></param>
/// <param name="ResourceOwner">The resource owner (a org.nr.).</param>
/// <param name="Identifier">The resource owner-unique identifier. Limited to 'a'-'z' and '-' characters.</param>
/// <param name="Name">The access list name. Does not have to be unique, and can contain any characters.</param>
/// <param name="Description">A access list description.</param>
/// <param name="CreatedAt">When this access list was created.</param>
/// <param name="UpdatedAt"><inheritdoc cref="AccessListMetadata(Guid, DateTimeOffset, ulong)" path="/param[@name='UpdatedAt']/node()"/></param>
/// <param name="ResourceConnections">The resource connections for the access list.</param>
/// <param name="Version"><inheritdoc cref="AccessListMetadata(Guid, DateTimeOffset, ulong)" path="/param[@name='Version']/node()"/></param>
public record AccessListInfo(
    Guid Id,
    string ResourceOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AccessListResourceConnection>? ResourceConnections,
    ulong Version)
    : AccessListMetadata(Id, UpdatedAt, Version);
