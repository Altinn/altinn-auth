using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Read access to the activity log over assignments, delegations and requests.
/// </summary>
public interface IActivityLogService
{
    /// <summary>
    /// Returns one page of activity log entries involving the given party, newest first.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries: From (given), To (received) or Via (facilitator);
    /// <see langword="null"/> matches any involvement. The party overwrites the corresponding filter field.</param>
    /// <param name="filter">Additional narrowing filters; InvolvedIds and the anchored field are overwritten by <paramref name="party"/>.</param>
    /// <param name="pageSize">Maximum number of entries per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="includeMps">Whether to include Maskinporten schema events. By default they are
    /// hidden by excluding the Supplier role, which is used exclusively for Maskinporten schema
    /// delegations — mirroring how connection queries hide them.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogPage> GetActivityLog(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, int pageSize, int pageNumber, bool includeMps = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of values occurring in the party's activity log entries for the given
    /// filter field, within the same filter semantics as <see cref="GetActivityLog"/>. The
    /// looked-up field's own filter list is ignored (so more values can be added to it); the
    /// party anchor never is.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries; <see langword="null"/> matches any involvement.</param>
    /// <param name="field">The field to return occurring values for.</param>
    /// <param name="filter">Additional narrowing filters; the looked-up field's own list is ignored.</param>
    /// <param name="term">Optional case-insensitive contains-match against the name.</param>
    /// <param name="orderBy">Value ordering.</param>
    /// <param name="pageSize">Maximum number of values per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="includeMps">Whether to include Maskinporten schema events (the Supplier role).
    /// By default they are hidden, so the Supplier role never appears as a filter value either.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogFilterValuePage> GetActivityLogFilterValues(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, bool includeMps = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// One page of activity log entries.
/// </summary>
public sealed record ActivityLogPage(IReadOnlyList<ActivityLogDto> Items, bool HasMore);

/// <summary>
/// One page of filter values.
/// </summary>
public sealed record ActivityLogFilterValuePage(IReadOnlyList<ActivityLogFilterValueDto> Items, bool HasMore);
