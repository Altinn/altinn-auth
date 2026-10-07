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
    public Task<ActivityLogPage> GetActivityLog(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        => GetCore(party, direction, filter, maskinportenSchema: false, pageSize, pageNumber, cancellationToken);

    /// <inheritdoc />
    public Task<ActivityLogPage> GetMaskinportenSchemaActivityLog(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        => GetCore(party, direction, filter, maskinportenSchema: true, pageSize, pageNumber, cancellationToken);

    /// <inheritdoc />
    public Task<ActivityLogFilterValuePage> GetActivityLogFilterValues(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, string languageCode = null, CancellationToken cancellationToken = default)
        => GetFilterValuesCore(party, direction, field, filter, maskinportenSchema: false, term, orderBy, pageSize, pageNumber, languageCode, cancellationToken);

    /// <inheritdoc />
    public Task<ActivityLogFilterValuePage> GetMaskinportenSchemaActivityLogFilterValues(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, string languageCode = null, CancellationToken cancellationToken = default)
        => GetFilterValuesCore(party, direction, field, filter, maskinportenSchema: true, term, orderBy, pageSize, pageNumber, languageCode, cancellationToken);

    private async Task<ActivityLogPage> GetCore(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, bool maskinportenSchema, int pageSize, int pageNumber, CancellationToken cancellationToken)
    {
        var sliced = Slice(filter ?? new ActivityLogQueryFilter(), maskinportenSchema);
        var anchoredFilter = Anchor(party, direction, sliced);

        var page = await activityLogQuery.GetAsync(anchoredFilter, pageSize, pageNumber, cancellationToken);

        var items = page.Items.Select(DtoMapper.Convert).ToList();
        return new ActivityLogPage(items, page.HasMore);
    }

    private async Task<ActivityLogFilterValuePage> GetFilterValuesCore(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, bool maskinportenSchema, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, string languageCode, CancellationToken cancellationToken)
    {
        // Slice after WithoutOwnField so a role-field lookup cannot escape the slice.
        var baseFilter = WithoutOwnField(field, filter ?? new ActivityLogQueryFilter());
        var anchoredFilter = Anchor(party, direction, Slice(baseFilter, maskinportenSchema));

        var page = await activityLogQuery.GetFilterValuesAsync(field, anchoredFilter, term, orderBy, pageSize, pageNumber, languageCode, cancellationToken);

        var items = page.Items.Select(DtoMapper.ToActivityLogFilterValueDto).ToList();
        return new ActivityLogFilterValuePage(items, page.HasMore);
    }

    // The Supplier role is used exclusively for Maskinporten schema delegations, so the two
    // slices are disjoint by construction: the regular surface always excludes the role (the
    // same rule connection queries apply), the maskinporten surface serves only it — no caller
    // combination can mix them. The exclusion survives WithoutOwnField, so the Supplier role
    // never shows up as a role filter value either.
    private static ActivityLogQueryFilter Slice(ActivityLogQueryFilter filter, bool maskinportenSchema)
    {
        if (maskinportenSchema)
        {
            return filter with { RoleIds = [RoleConstants.Supplier.Id], ExcludeRoleIds = null };
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
