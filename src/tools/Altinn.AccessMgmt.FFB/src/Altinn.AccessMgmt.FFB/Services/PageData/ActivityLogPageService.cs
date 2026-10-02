using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Runs ActivityLogQuery against an environment for the activity log page: the entry query
/// and the filter value lookups its filter pickers use.
/// </summary>
public sealed class ActivityLogPageService(IEnvironmentDbContextFactory dbFactory)
{
    public async Task<ActivityLogQueryPage> QueryAsync(
        string environment,
        ActivityLogQueryFilter filter,
        int pageSize,
        int pageNumber,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);
        var query = new ActivityLogQuery(db);

        return await query.GetAsync(filter, pageSize, pageNumber, ct);
    }

    public async Task<ActivityLogFilterValueQueryPage> FilterValuesAsync(
        string environment,
        ActivityLogFilterField field,
        ActivityLogQueryFilter filter,
        string? term,
        int pageSize,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);
        var query = new ActivityLogQuery(db);

        return await query.GetFilterValuesAsync(field, filter, term, ActivityLogFilterValueOrder.Name, pageSize, 0, ct);
    }
}
