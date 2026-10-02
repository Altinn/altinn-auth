using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// Ordering of filter values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLogFilterValueOrder
{
    /// <summary>
    /// Alphabetically by name — stable across pages.
    /// </summary>
    Name = 1,

    /// <summary>
    /// Newest occurrence first (per value pair) — new events can shift pages.
    /// </summary>
    When = 2,
}
