using System.ComponentModel.DataAnnotations;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// The query parameters shared by every activity log query endpoint, bound from the query
/// string by property name. Every list accepts repeated values; values within one parameter
/// are OR'ed while different parameters are AND'ed.
/// </summary>
public class ActivityLogQueryParameters
{
    /// <summary>
    /// The party that must be involved in every entry. This is the authorization anchor.
    /// </summary>
    [Required]
    public Guid Party { get; set; }

    /// <summary>
    /// How the party anchors the entries: From (given), To (received) or Via (facilitator).
    /// Without it any involvement matches.
    /// </summary>
    public ActivityLogDirection? Direction { get; set; }

    /// <summary>
    /// Activity type catalog ids, each expanding to its whole type/subtype/trigger/status
    /// combination; combinations are OR'ed.
    /// </summary>
    public List<Guid> TypeId { get; set; }

    /// <summary>
    /// Main record types.
    /// </summary>
    public List<ActivityLogType> Type { get; set; }

    /// <summary>
    /// Child record types.
    /// </summary>
    public List<ActivityLogSubtype> Subtype { get; set; }

    /// <summary>
    /// The operations that produced the entries.
    /// </summary>
    public List<ActivityLogTrigger> Trigger { get; set; }

    /// <summary>
    /// Request statuses.
    /// </summary>
    public List<RequestStatus> Status { get; set; }

    /// <summary>
    /// Acting entity identifiers.
    /// </summary>
    public List<Guid> By { get; set; }

    /// <summary>
    /// Source system identifiers.
    /// </summary>
    public List<Guid> Source { get; set; }

    /// <summary>
    /// Change operation identifiers.
    /// </summary>
    public List<string> Operation { get; set; }

    /// <summary>
    /// From-party identifiers.
    /// </summary>
    public List<Guid> From { get; set; }

    /// <summary>
    /// To-party identifiers.
    /// </summary>
    public List<Guid> To { get; set; }

    /// <summary>
    /// Facilitator party identifiers.
    /// </summary>
    public List<Guid> Via { get; set; }

    /// <summary>
    /// Role identifiers.
    /// </summary>
    public List<Guid> Role { get; set; }

    /// <summary>
    /// Access package identifiers.
    /// </summary>
    public List<Guid> Package { get; set; }

    /// <summary>
    /// Resource identifiers.
    /// </summary>
    public List<Guid> Resource { get; set; }

    /// <summary>
    /// Instance URNs.
    /// </summary>
    public List<string> Instance { get; set; }

    /// <summary>
    /// Identifiers of the affected rows.
    /// </summary>
    public List<Guid> ItemId { get; set; }

    /// <summary>
    /// Identifiers of the affected rows' main records.
    /// </summary>
    public List<Guid> ParentId { get; set; }

    /// <summary>
    /// Inclusive lower bound for the entry time.
    /// </summary>
    public DateTimeOffset? After { get; set; }

    /// <summary>
    /// Exclusive upper bound for the entry time.
    /// </summary>
    public DateTimeOffset? Before { get; set; }

    /// <summary>
    /// Maximum number of entries per page (default 100, clamped to 1–1000).
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Zero-based page number.
    /// </summary>
    public int? PageNo { get; set; }
}

/// <summary>
/// The query parameters for the filter value endpoints: the shared query surface plus the
/// name search term and value ordering.
/// </summary>
public class ActivityLogFilterValueQueryParameters : ActivityLogQueryParameters
{
    /// <summary>
    /// Case-insensitive contains-match against the value name.
    /// </summary>
    public string Term { get; set; }

    /// <summary>
    /// Value ordering: Name (default, stable across pages) or When (newest first).
    /// </summary>
    public ActivityLogFilterValueOrder OrderBy { get; set; } = ActivityLogFilterValueOrder.Name;
}
