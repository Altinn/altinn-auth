using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.PersistenceEF.Queries;

/// <summary>
/// Represents a filter for querying activity log entries. Every collection is optional; a set
/// collection narrows the result to entries matching any of its values.
/// </summary>
public sealed record ActivityLogQueryFilter
{
    /// <summary>
    /// Gets the collection of party identifiers that must be involved in the entry as from,
    /// to or via. This is the enduser authorization anchor.
    /// </summary>
    public IReadOnlyCollection<Guid> InvolvedIds { get; init; }

    /// <summary>
    /// Gets the collection of party identifiers that must appear in the entry as from, to,
    /// via or the acting entity (by).
    /// </summary>
    public IReadOnlyCollection<Guid> AnyPartyIds { get; init; }

    /// <summary>
    /// Gets the collection of activity type catalog keys, each matched as a whole conjunction
    /// and OR'ed together — unlike the independent value lists below, which cross-combine.
    /// </summary>
    public IReadOnlyCollection<ActivityTypeKey> ActivityTypeKeys { get; init; }

    /// <summary>
    /// Gets the collection of main record types.
    /// </summary>
    public IReadOnlyCollection<ActivityLogType> Types { get; init; }

    /// <summary>
    /// Gets the collection of child record types.
    /// </summary>
    public IReadOnlyCollection<ActivityLogSubtype> Subtypes { get; init; }

    /// <summary>
    /// Gets the collection of database operations.
    /// </summary>
    public IReadOnlyCollection<ActivityLogTrigger> Triggers { get; init; }

    /// <summary>
    /// Gets the collection of request statuses.
    /// </summary>
    public IReadOnlyCollection<RequestStatus> Statuses { get; init; }

    /// <summary>
    /// Gets the collection of acting entity identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ByIds { get; init; }

    /// <summary>
    /// Gets the collection of source system identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> SourceIds { get; init; }

    /// <summary>
    /// Gets the collection of change operation identifiers.
    /// </summary>
    public IReadOnlyCollection<string> OperationIds { get; init; }

    /// <summary>
    /// Gets the collection of from-party identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> FromIds { get; init; }

    /// <summary>
    /// Gets the collection of to-party identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ToIds { get; init; }

    /// <summary>
    /// Gets the collection of facilitator party identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ViaIds { get; init; }

    /// <summary>
    /// Gets the collection of role identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> RoleIds { get; init; }

    /// <summary>
    /// Gets the collection of access package identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> PackageIds { get; init; }

    /// <summary>
    /// Gets the collection of resource identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ResourceIds { get; init; }

    /// <summary>
    /// Gets the collection of instance URNs.
    /// </summary>
    public IReadOnlyCollection<string> InstanceIds { get; init; }

    /// <summary>
    /// Gets the collection of affected row identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ItemIds { get; init; }

    /// <summary>
    /// Gets the collection of main record identifiers for child entries.
    /// </summary>
    public IReadOnlyCollection<Guid> ParentIds { get; init; }

    /// <summary>
    /// Gets the inclusive lower bound for the entry time.
    /// </summary>
    public DateTimeOffset? After { get; init; }

    /// <summary>
    /// Gets the exclusive upper bound for the entry time.
    /// </summary>
    public DateTimeOffset? Before { get; init; }

    /// <summary>
    /// Returns true if at least one filter that genuinely narrows the scan is provided.
    /// </summary>
    public bool HasAny =>
        InvolvedIds?.Count > 0 ||
        AnyPartyIds?.Count > 0 ||
        FromIds?.Count > 0 ||
        ToIds?.Count > 0 ||
        ViaIds?.Count > 0 ||
        ByIds?.Count > 0 ||
        ItemIds?.Count > 0 ||
        ParentIds?.Count > 0 ||
        OperationIds?.Count > 0 ||
        After.HasValue ||
        Before.HasValue;

    /// <summary>
    /// Ensures that at least one narrowing filter parameter is set.
    /// </summary>
    public void Validate()
    {
        if (!HasAny)
        {
            throw new ArgumentException("At least one narrowing filter parameter must be set.");
        }
    }
}
