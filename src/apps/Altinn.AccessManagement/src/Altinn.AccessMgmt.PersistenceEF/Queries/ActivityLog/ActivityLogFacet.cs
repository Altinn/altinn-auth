namespace Altinn.AccessMgmt.PersistenceEF.Queries;

/// <summary>
/// One value occurring in the activity log for a facet field. Pairs are distinct on
/// (id, name), so the same id can recur with different name snapshots.
/// </summary>
public sealed record ActivityLogFacet(Guid Id, string Name);

/// <summary>
/// One page of facet values.
/// </summary>
public sealed class ActivityLogFacetQueryPage(IReadOnlyList<ActivityLogFacet> items, bool hasMore)
{
    /// <summary>
    /// The values, in the requested order.
    /// </summary>
    public IReadOnlyList<ActivityLogFacet> Items { get; } = items;

    /// <summary>
    /// Whether more values exist beyond this page.
    /// </summary>
    public bool HasMore { get; } = hasMore;
}
