#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Information about an access list.
/// </summary>
/// <param name="Id">The database id for the access list.</param>
/// <param name="ResourceOwner">The resource owner (a org.nr.).</param>
/// <param name="Identifier">The resource owner-unique identifier. Limited to 'a'-'z' and '-' characters.</param>
/// <param name="Name">The access list name. Does not have to be unique, and can contain any characters.</param>
/// <param name="Description">A access list description.</param>
/// <param name="CreatedAt">When this access list was created.</param>
/// <param name="UpdatedAt">When this access list was last updated.</param>
/// <param name="ResourceConnections">The resource connections for the access list.</param>
/// <param name="Version">The version of this access list.</param>
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
    : IVersionEquatable<ulong>
{
    /// <inheritdoc/>
    bool IVersionEquatable<ulong>.ModifiedSince(HttpDateTimeHeaderValue other)
    {
        return UpdatedAt > other;
    }

    /// <inheritdoc/>
    bool IVersionEquatable<ulong>.VersionEquals(ulong other)
    {
        return Version == other;
    }
}
