#nullable enable

namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// Utilities for <see cref="VersionedPage{TItem, TToken, TVersionTag}"/>.
/// </summary>
public static class VersionedPage
{
    /// <summary>
    /// Creates a versioned page of items.
    /// </summary>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <typeparam name="TToken">The continuation token type.</typeparam>
    /// <typeparam name="TVersionTag">The verison tag type.</typeparam>
    /// <param name="page">The page.</param>
    /// <param name="modifiedAt">When the page was last modified.</param>
    /// <param name="versionTag">The page version.</param>
    /// <returns>A <see cref="VersionedPage{TItem, TToken, TVersionTag}"/>.</returns>
    public static VersionedPage<TItem, TToken, TVersionTag> WithVersion<TItem, TToken, TVersionTag>(
        this Page<TItem, TToken> page,
        DateTimeOffset modifiedAt,
        TVersionTag versionTag)
        => new(page.Items, page.ContinuationToken, versionTag, modifiedAt);
}

/// <summary>
/// A page of items belonging to a versioned entity.
/// </summary>
/// <typeparam name="TItem">The item type</typeparam>
/// <typeparam name="TToken">The token type used to request the next page</typeparam>
/// <typeparam name="TVersionTag">The version tag type.</typeparam>
public class VersionedPage<TItem, TToken, TVersionTag>(
    IReadOnlyList<TItem> items,
    Optional<TToken> continuationToken,
    TVersionTag versionTag,
    DateTimeOffset modifiedAt)
    : Page<TItem, TToken>(items, continuationToken)
{
    /// <summary>
    /// Gets the version of the entity the page is for.
    /// </summary>
    public TVersionTag Version => versionTag;

    /// <summary>
    /// Gets the last modified time of the entity the page is for.
    /// </summary>
    public DateTimeOffset ModifiedAt => modifiedAt;
}
