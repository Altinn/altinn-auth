using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Queries;

/// <summary>
/// Queries <c>dbo.activitylog</c> with multi-value filtering and page-based pagination
/// ordered by <c>("when", id)</c> descending.
/// </summary>
public sealed class ActivityLogQuery(AppDbContext db)
{
    /// <summary>
    /// Returns one page of activity log entries matching the filter, newest first.
    /// </summary>
    /// <param name="filter">The filter; at least one narrowing parameter must be set.</param>
    /// <param name="pageSize">Maximum number of entries to return.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ActivityLogQueryPage> GetAsync(
        ActivityLogQueryFilter filter,
        int pageSize,
        int pageNumber = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);
        filter.Validate();

        var items = await BuildQuery(filter)
            .OrderByDescending(t => t.When)
            .ThenByDescending(t => t.Id)
            .Skip(pageNumber * pageSize)
            .Take(pageSize + 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(pageSize);
        }

        return new ActivityLogQueryPage(items, hasMore);
    }

    private IQueryable<ActivityLog> BuildQuery(ActivityLogQueryFilter filter)
    {
        return db.ActivityLogs
            .InvolvedIdContains(ToSet(filter.InvolvedIds))
            .AnyPartyIdContains(ToSet(filter.AnyPartyIds))
            .ActivityTypeKeyContains(filter.ActivityTypeKeys)
            .TypeContains(ToSet(filter.Types))
            .SubtypeContains(ToSet(filter.Subtypes))
            .TriggerContains(ToSet(filter.Triggers))
            .StatusContains(ToSet(filter.Statuses))
            .ByIdContains(ToSet(filter.ByIds))
            .SourceIdContains(ToSet(filter.SourceIds))
            .OperationIdContains(ToSet(filter.OperationIds))
            .FromIdContains(ToSet(filter.FromIds))
            .ToIdContains(ToSet(filter.ToIds))
            .ViaIdContains(ToSet(filter.ViaIds))
            .RoleIdContains(ToSet(filter.RoleIds))
            .PackageIdContains(ToSet(filter.PackageIds))
            .ResourceIdContains(ToSet(filter.ResourceIds))
            .InstanceIdContains(ToSet(filter.InstanceIds))
            .ItemIdContains(ToSet(filter.ItemIds))
            .ParentIdContains(ToSet(filter.ParentIds))
            .WhereIf(filter.After.HasValue, t => t.When >= filter.After.Value)
            .WhereIf(filter.Before.HasValue, t => t.When < filter.Before.Value);
    }

    private static HashSet<T> ToSet<T>(IReadOnlyCollection<T> values)
        => values?.Count > 0 ? new HashSet<T>(values) : null;
}

/// <summary>
/// One page of activity log entries.
/// </summary>
public sealed class ActivityLogQueryPage(IReadOnlyList<ActivityLog> items, bool hasMore)
{
    /// <summary>
    /// The entries, ordered newest first.
    /// </summary>
    public IReadOnlyList<ActivityLog> Items { get; } = items;

    /// <summary>
    /// Whether more entries exist beyond this page.
    /// </summary>
    public bool HasMore { get; } = hasMore;
}
