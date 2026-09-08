using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// Ordering of facet values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLogFacetOrder
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
