using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public class ActivityLogService(ActivityLogQuery activityLogQuery) : IActivityLogService
{
    /// <inheritdoc />
    public async Task<ActivityLogPage> GetActivityLog(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, int pageSize, int pageNumber, bool includeMps = false, CancellationToken cancellationToken = default)
    {
        var anchoredFilter = WithoutMaskinportenSchema(Anchor(party, direction, filter ?? new ActivityLogQueryFilter()), includeMps);

        var page = await activityLogQuery.GetAsync(anchoredFilter, pageSize, pageNumber, cancellationToken);

        var items = page.Items.Select(DtoMapper.Convert).ToList();
        return new ActivityLogPage(items, page.HasMore);
    }

    /// <inheritdoc />
    public async Task<ActivityLogFilterValuePage> GetActivityLogFilterValues(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, bool includeMps = false, CancellationToken cancellationToken = default)
    {
        var baseFilter = WithoutOwnField(field, filter ?? new ActivityLogQueryFilter());
        var anchoredFilter = WithoutMaskinportenSchema(Anchor(party, direction, baseFilter), includeMps);

        var page = await activityLogQuery.GetFilterValuesAsync(field, anchoredFilter, term, orderBy, pageSize, pageNumber, cancellationToken);

        var items = page.Items.Select(DtoMapper.ToActivityLogFilterValueDto).ToList();
        return new ActivityLogFilterValuePage(items, page.HasMore);
    }

    // The Supplier role is used exclusively for Maskinporten schema delegations, so excluding
    // it hides those events entirely — the same rule connection queries apply. The exclusion
    // survives WithoutOwnField, so the Supplier role never shows up as a role filter value.
    private static ActivityLogQueryFilter WithoutMaskinportenSchema(ActivityLogQueryFilter filter, bool includeMps)
    {
        if (includeMps)
        {
            return filter;
        }

        IReadOnlyCollection<Guid> excluded = filter.ExcludeRoleIds is { Count: > 0 }
            ? [.. filter.ExcludeRoleIds, RoleConstants.Supplier.Id]
            : [RoleConstants.Supplier.Id];

        return filter with { ExcludeRoleIds = excluded };
    }

    private static ActivityLogQueryFilter Anchor(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter)
    {
        if (party == Guid.Empty)
        {
            throw new ArgumentException("Party must be a non-empty guid.", nameof(party));
        }

        return direction switch
        {
            ActivityLogDirection.From => filter with { FromIds = [party], InvolvedIds = null },
            ActivityLogDirection.To => filter with { ToIds = [party], InvolvedIds = null },
            ActivityLogDirection.Via => filter with { ViaIds = [party], InvolvedIds = null },
            _ => filter with { InvolvedIds = [party] },
        };
    }

    private static ActivityLogQueryFilter WithoutOwnField(ActivityLogFilterField field, ActivityLogQueryFilter filter) => field switch
    {
        ActivityLogFilterField.From => filter with { FromIds = null },
        ActivityLogFilterField.To => filter with { ToIds = null },
        ActivityLogFilterField.Via => filter with { ViaIds = null },
        ActivityLogFilterField.By => filter with { ByIds = null },
        ActivityLogFilterField.Role => filter with { RoleIds = null },
        ActivityLogFilterField.Package => filter with { PackageIds = null },
        ActivityLogFilterField.Resource => filter with { ResourceIds = null },
        ActivityLogFilterField.Source => filter with { SourceIds = null },
        ActivityLogFilterField.ActivityType => filter with { ActivityTypeKeys = null },
        _ => filter,
    };
}
