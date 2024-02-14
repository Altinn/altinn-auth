#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Utils for <see cref="VersionedPaginated{T, TVersionTag}"/>.
/// </summary>
public static class VersionedPaginated
{
    /// <summary>
    /// Create a new <see cref="VersionedPaginated{T, TVersionTag}"/> from a <see cref="Paginated{T}"/>
    /// and version information.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <typeparam name="TVersionTag">The version tag.</typeparam>
    /// <param name="paginated">The paginated data.</param>
    /// <param name="lastModified">When the page was last modified.</param>
    /// <param name="version">The page version.</param>
    /// <returns>A new <see cref="VersionedPaginated{T, TVersionTag}"/>.</returns>
    public static VersionedPaginated<T, TVersionTag> WithVersion<T, TVersionTag>(
        this Paginated<T> paginated,
        DateTimeOffset lastModified,
        TVersionTag version)
        => new(paginated.Links, paginated.Items, lastModified, version);
}

/// <summary>
/// A paginated <see cref="ListObject{T}"/> with version information.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <typeparam name="TVersionTag">The version tag type.</typeparam>
/// <param name="Links">Pagination links.</param>
/// <param name="Items">The items.</param>
/// <param name="LastModified">When this page was last modified.</param>
/// <param name="Version">The version of this page.</param>
public record VersionedPaginated<T, TVersionTag>(
    PaginatedLinks Links,
    IEnumerable<T> Items,
    [property: JsonIgnore]
    DateTimeOffset LastModified,
    [property: JsonIgnore]
    TVersionTag Version)
    : Paginated<T>(Links, Items)
    , ITaggedEntity<TVersionTag>
{
    /// <inheritdoc/>
    void ITaggedEntity<TVersionTag>.GetHeaderValues(out TVersionTag version, out HttpDateTimeHeaderValue modifiedAt)
    {
        version = Version;
        modifiedAt = new HttpDateTimeHeaderValue(LastModified);
    }
}
