#nullable enable

using System.Collections.Immutable;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Information about an access list resource connection.
/// </summary>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The allowed actions.</param>
/// <param name="Created">When the connection was created.</param>
/// <param name="Modified">When the connection was last modified.</param>
public record AccessListResourceConnection(
    string ResourceIdentifier,
    ImmutableHashSet<string>? Actions,
    DateTimeOffset Created,
    DateTimeOffset Modified);
