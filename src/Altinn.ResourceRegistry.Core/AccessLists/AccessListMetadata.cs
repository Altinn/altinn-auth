#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Metadata about an access list.
/// </summary>
/// <param name="Id">The database id for the access list.</param>
/// <param name="UpdatedAt">When this access list was last updated.</param>
/// <param name="Version">The version of this access list.</param>
public record AccessListMetadata(
    Guid Id,
    DateTimeOffset UpdatedAt,
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
