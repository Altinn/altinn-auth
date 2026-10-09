using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Read access to the activity log over assignments, delegations and requests, exposed as one
/// surface per area. Each surface structurally clamps the query to its slice — requested
/// types or catalog combinations outside it narrow to an empty page, never widen — and the
/// Maskinporten schema slice (the Supplier role, used exclusively for those delegations) is
/// disjoint from the rest: the connections and request surfaces always exclude it, the
/// maskinporten surface serves only it. No caller combination can mix the slices.
/// </summary>
public interface IActivityLogService
{
    /// <summary>
    /// Returns one page of assignment and delegation events involving the given party, newest
    /// first. Maskinporten schema events are always excluded.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries: From (given), To (received) or Via (facilitator);
    /// <see langword="null"/> matches any involvement. The party overwrites the corresponding filter field.</param>
    /// <param name="filter">Additional narrowing filters; InvolvedIds and the anchored field are overwritten by <paramref name="party"/>.</param>
    /// <param name="pageSize">Maximum number of entries per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogPage> GetConnectionsActivityLog(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, int pageSize, int pageNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of access request events involving the given party, newest first.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries; <see langword="null"/> matches any involvement.</param>
    /// <param name="filter">Additional narrowing filters; InvolvedIds and the anchored field are overwritten by <paramref name="party"/>.</param>
    /// <param name="pageSize">Maximum number of entries per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogPage> GetRequestActivityLog(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, int pageSize, int pageNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of Maskinporten schema events involving the given party, newest
    /// first: only Supplier-role events, the slice every other surface hides. Any role filter
    /// on <paramref name="filter"/> is overwritten by the Supplier role.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries; <see langword="null"/> matches any involvement.</param>
    /// <param name="filter">Additional narrowing filters; InvolvedIds, the anchored field and the role list are overwritten.</param>
    /// <param name="pageSize">Maximum number of entries per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogPage> GetMaskinportenSchemaActivityLog(Guid party, ActivityLogDirection? direction, ActivityLogQueryFilter filter, int pageSize, int pageNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of values occurring in the party's slice of the connections surface
    /// for the given filter field, within the same filter semantics as
    /// <see cref="GetConnectionsActivityLog"/> — the Supplier role never appears as a value.
    /// The looked-up field's own filter list is ignored (so more values can be added to it);
    /// the party anchor never is.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries; <see langword="null"/> matches any involvement.</param>
    /// <param name="field">The field to return occurring values for.</param>
    /// <param name="filter">Additional narrowing filters; the looked-up field's own list is ignored.</param>
    /// <param name="term">Optional case-insensitive contains-match against the name.</param>
    /// <param name="orderBy">Value ordering.</param>
    /// <param name="pageSize">Maximum number of values per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="languageCode">Three-letter language code ("eng", "nno") for catalog-backed
    /// value names (the activity type field); applied before term matching and ordering.
    /// Null means bokmål. Snapshot names are data and are never translated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogFilterValuePage> GetConnectionsActivityLogFilterValues(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, string languageCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of values occurring in the party's slice of the request surface for
    /// the given filter field, within the same filter semantics as
    /// <see cref="GetRequestActivityLog"/>.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries; <see langword="null"/> matches any involvement.</param>
    /// <param name="field">The field to return occurring values for.</param>
    /// <param name="filter">Additional narrowing filters; the looked-up field's own list is ignored.</param>
    /// <param name="term">Optional case-insensitive contains-match against the name.</param>
    /// <param name="orderBy">Value ordering.</param>
    /// <param name="pageSize">Maximum number of values per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="languageCode">Three-letter language code ("eng", "nno") for catalog-backed value names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogFilterValuePage> GetRequestActivityLogFilterValues(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, string languageCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of values occurring in the party's Maskinporten schema events for the
    /// given filter field, within the same filter semantics as
    /// <see cref="GetMaskinportenSchemaActivityLog"/> — only Supplier-role events contribute.
    /// </summary>
    /// <param name="party">The party that must be involved in every entry. This is the authorization anchor.</param>
    /// <param name="direction">How the party anchors the entries; <see langword="null"/> matches any involvement.</param>
    /// <param name="field">The field to return occurring values for.</param>
    /// <param name="filter">Additional narrowing filters; the looked-up field's own list is ignored and the role list is overwritten.</param>
    /// <param name="term">Optional case-insensitive contains-match against the name.</param>
    /// <param name="orderBy">Value ordering.</param>
    /// <param name="pageSize">Maximum number of values per page.</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="languageCode">Three-letter language code ("eng", "nno") for catalog-backed value names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ActivityLogFilterValuePage> GetMaskinportenSchemaActivityLogFilterValues(Guid party, ActivityLogDirection? direction, ActivityLogFilterField field, ActivityLogQueryFilter filter, string term, ActivityLogFilterValueOrder orderBy, int pageSize, int pageNumber, string languageCode = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// One page of activity log entries.
/// </summary>
public sealed record ActivityLogPage(IReadOnlyList<ActivityLogDto> Items, bool HasMore);

/// <summary>
/// One page of filter values.
/// </summary>
public sealed record ActivityLogFilterValuePage(IReadOnlyList<ActivityLogFilterValueDto> Items, bool HasMore);
