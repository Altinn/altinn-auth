using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// How the requesting party anchors the activity log entries: on the from-side (access given),
/// the to-side (access received), or as facilitator. When no direction is given, entries with
/// any involvement are returned.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLogDirection
{
    /// <summary>
    /// The party is the from-party of the entries (access given).
    /// </summary>
    From = 1,

    /// <summary>
    /// The party is the to-party of the entries (access received).
    /// </summary>
    To = 2,

    /// <summary>
    /// The party is the facilitator of the entries (delegations handled via the party).
    /// </summary>
    Via = 3,
}
