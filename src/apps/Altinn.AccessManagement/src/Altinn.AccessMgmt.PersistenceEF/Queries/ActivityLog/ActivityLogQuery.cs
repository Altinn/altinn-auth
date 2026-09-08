using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
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

    /// <summary>
    /// Returns one page of values occurring in the log for the given facet field, within the
    /// same filter semantics as <see cref="GetAsync"/>. Values are distinct (id, name) pairs;
    /// the same id can recur with different name snapshots. The term matches names only.
    /// </summary>
    /// <param name="field">The field to return occurring values for.</param>
    /// <param name="filter">The filter; at least one narrowing parameter must be set.</param>
    /// <param name="term">Optional case-insensitive contains-match against the name.</param>
    /// <param name="orderBy">Value ordering; name is stable across pages, when is newest first.</param>
    /// <param name="pageSize">Maximum number of values to return.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ActivityLogFacetQueryPage> GetFacetAsync(
        ActivityLogFacetField field,
        ActivityLogQueryFilter filter,
        string term,
        ActivityLogFacetOrder orderBy,
        int pageSize,
        int pageNumber = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);
        filter.Validate();

        var source = BuildQuery(filter);

        return field switch
        {
            ActivityLogFacetField.From => await PageSnapshotFacetAsync(source.Where(t => t.FromId != null).Select(t => new FacetRow { Id = t.FromId.Value, Name = t.FromName, When = t.When }), term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.To => await PageSnapshotFacetAsync(source.Where(t => t.ToId != null).Select(t => new FacetRow { Id = t.ToId.Value, Name = t.ToName, When = t.When }), term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.Via => await PageSnapshotFacetAsync(source.Where(t => t.ViaId != null).Select(t => new FacetRow { Id = t.ViaId.Value, Name = t.ViaName, When = t.When }), term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.By => await PageSnapshotFacetAsync(source.Where(t => t.ById != null).Select(t => new FacetRow { Id = t.ById.Value, Name = t.ByName, When = t.When }), term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.Role => await PageSnapshotFacetAsync(source.Where(t => t.RoleId != null).Select(t => new FacetRow { Id = t.RoleId.Value, Name = t.RoleName, When = t.When }), term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.Package => await PageSnapshotFacetAsync(source.Where(t => t.PackageId != null).Select(t => new FacetRow { Id = t.PackageId.Value, Name = t.PackageName, When = t.When }), term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.Resource => await PageSnapshotFacetAsync(source.Where(t => t.ResourceId != null).Select(t => new FacetRow { Id = t.ResourceId.Value, Name = t.ResourceName, When = t.When }), term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.Source => await PageSourceFacetAsync(source, term, orderBy, pageSize, pageNumber, cancellationToken),
            ActivityLogFacetField.ActivityType => await PageActivityTypeFacetAsync(source, term, orderBy, pageSize, pageNumber, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown facet field."),
        };
    }

    private static async Task<ActivityLogFacetQueryPage> PageSnapshotFacetAsync(
        IQueryable<FacetRow> rows,
        string term,
        ActivityLogFacetOrder orderBy,
        int pageSize,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{EscapeLike(term.Trim())}%";
            rows = rows.Where(r => r.Name != null && EF.Functions.ILike(r.Name, pattern, "\\"));
        }

        var grouped = rows
            .GroupBy(r => new { r.Id, r.Name })
            .Select(g => new { g.Key.Id, g.Key.Name, When = g.Max(r => r.When) });

        grouped = orderBy == ActivityLogFacetOrder.When
            ? grouped.OrderByDescending(r => r.When).ThenBy(r => r.Name).ThenBy(r => r.Id)
            : grouped.OrderBy(r => r.Name).ThenBy(r => r.Id);

        var page = await grouped
            .Skip(pageNumber * pageSize)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        if (hasMore)
        {
            page.RemoveAt(pageSize);
        }

        return new ActivityLogFacetQueryPage(page.Select(r => new ActivityLogFacet(r.Id, r.Name)).ToList(), hasMore);
    }

    private static async Task<ActivityLogFacetQueryPage> PageSourceFacetAsync(
        IQueryable<ActivityLog> source,
        string term,
        ActivityLogFacetOrder orderBy,
        int pageSize,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        var occurring = await source
            .Where(t => t.SourceId != null)
            .GroupBy(t => t.SourceId)
            .Select(g => new { Id = g.Key.Value, When = g.Max(t => t.When) })
            .ToListAsync(cancellationToken);

        var values = occurring.Select(s => (
            s.Id,
            Name: SystemEntityConstants.TryGetById(s.Id, out var definition) ? definition.Entity.Name : null,
            s.When));

        return PageInMemory(values, term, orderBy, pageSize, pageNumber);
    }

    private static async Task<ActivityLogFacetQueryPage> PageActivityTypeFacetAsync(
        IQueryable<ActivityLog> source,
        string term,
        ActivityLogFacetOrder orderBy,
        int pageSize,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        var combos = await source
            .GroupBy(t => new { t.Type, t.Subtype, t.Trigger, t.Status })
            .Select(g => new { g.Key.Type, g.Key.Subtype, g.Key.Trigger, g.Key.Status, When = g.Max(t => t.When) })
            .ToListAsync(cancellationToken);

        var values = combos
            .Select(c => (Definition: ActivityTypeConstants.Resolve(c.Type, c.Subtype, c.Trigger, c.Status), c.When))
            .Where(x => x.Definition is not null)
            .GroupBy(x => x.Definition.Id)
            .Select(g => (
                Id: g.Key,
                Name: g.First().Definition.Entity.Name,
                When: g.Max(x => x.When)));

        return PageInMemory(values, term, orderBy, pageSize, pageNumber);
    }

    private static ActivityLogFacetQueryPage PageInMemory(
        IEnumerable<(Guid Id, string Name, DateTimeOffset When)> values,
        string term,
        ActivityLogFacetOrder orderBy,
        int pageSize,
        int pageNumber)
    {
        if (!string.IsNullOrWhiteSpace(term))
        {
            var trimmed = term.Trim();
            values = values.Where(v => v.Name is not null && v.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
        }

        values = orderBy == ActivityLogFacetOrder.When
            ? values.OrderByDescending(v => v.When).ThenBy(v => v.Name, StringComparer.Ordinal).ThenBy(v => v.Id)
            : values.OrderBy(v => v.Name, StringComparer.Ordinal).ThenBy(v => v.Id);

        var page = values.Skip(pageNumber * pageSize).Take(pageSize + 1).ToList();

        var hasMore = page.Count > pageSize;
        if (hasMore)
        {
            page.RemoveAt(pageSize);
        }

        return new ActivityLogFacetQueryPage(page.Select(v => new ActivityLogFacet(v.Id, v.Name)).ToList(), hasMore);
    }

    private static string EscapeLike(string term)
        => term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static HashSet<T> ToSet<T>(IReadOnlyCollection<T> values)
        => values?.Count > 0 ? new HashSet<T>(values) : null;

    // Member-init shape on purpose: EF only inlines member access over anonymous types and
    // member-init projections; a positional record constructor is untranslatable in GroupBy.
    private sealed record FacetRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; }

        public DateTimeOffset When { get; init; }
    }
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
