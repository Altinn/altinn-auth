namespace Altinn.AccessMgmt.PersistenceEF.Queries;

/// <summary>
/// One value occurring in the activity log for a filter field. Pairs are distinct on
/// (id, name), so the same id can recur with different name snapshots.
/// </summary>
public sealed record ActivityLogFilterValue(Guid Id, string Name);

/// <summary>
/// One page of filter values.
/// </summary>
public sealed class ActivityLogFilterValueQueryPage(IReadOnlyList<ActivityLogFilterValue> items, bool hasMore)
{
    /// <summary>
    /// The values, in the requested order.
    /// </summary>
    public IReadOnlyList<ActivityLogFilterValue> Items { get; } = items;

    /// <summary>
    /// Whether more values exist beyond this page.
    /// </summary>
    public bool HasMore { get; } = hasMore;
}
