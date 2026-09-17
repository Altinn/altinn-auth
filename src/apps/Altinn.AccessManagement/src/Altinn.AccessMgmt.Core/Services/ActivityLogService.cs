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
    public async Task<ActivityLogFacetPage> GetActivityLogFacet(Guid party, ActivityLogDirection? direction, ActivityLogFacetField field, ActivityLogQueryFilter filter, string term, ActivityLogFacetOrder orderBy, int pageSize, int pageNumber, bool includeMps = false, CancellationToken cancellationToken = default)
    {
        var baseFilter = WithoutOwnField(field, filter ?? new ActivityLogQueryFilter());
        var anchoredFilter = WithoutMaskinportenSchema(Anchor(party, direction, baseFilter), includeMps);

        var page = await activityLogQuery.GetFacetAsync(field, anchoredFilter, term, orderBy, pageSize, pageNumber, cancellationToken);

        var items = page.Items.Select(DtoMapper.ToActivityLogFacetDto).ToList();
        return new ActivityLogFacetPage(items, page.HasMore);
    }

    // The Supplier role is used exclusively for Maskinporten schema delegations, so excluding
    // it hides those events entirely — the same rule connection queries apply. The exclusion
    // survives WithoutOwnField, so the Supplier role never shows up as a role facet value.
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

    private static ActivityLogQueryFilter WithoutOwnField(ActivityLogFacetField field, ActivityLogQueryFilter filter) => field switch
    {
        ActivityLogFacetField.From => filter with { FromIds = null },
        ActivityLogFacetField.To => filter with { ToIds = null },
        ActivityLogFacetField.Via => filter with { ViaIds = null },
        ActivityLogFacetField.By => filter with { ByIds = null },
        ActivityLogFacetField.Role => filter with { RoleIds = null },
        ActivityLogFacetField.Package => filter with { PackageIds = null },
        ActivityLogFacetField.Resource => filter with { ResourceIds = null },
        ActivityLogFacetField.Source => filter with { SourceIds = null },
        ActivityLogFacetField.ActivityType => filter with { ActivityTypeKeys = null },
        _ => filter,
    };
}
