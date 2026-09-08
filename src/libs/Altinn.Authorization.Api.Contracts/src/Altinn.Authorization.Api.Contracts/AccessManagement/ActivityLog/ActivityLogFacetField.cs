using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// The activity log field a facet lookup returns occurring values for.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLogFacetField
{
    /// <summary>
    /// The from-party.
    /// </summary>
    From = 1,

    /// <summary>
    /// The to-party.
    /// </summary>
    To = 2,

    /// <summary>
    /// The facilitator party.
    /// </summary>
    Via = 3,

    /// <summary>
    /// The acting entity.
    /// </summary>
    By = 4,

    /// <summary>
    /// The role.
    /// </summary>
    Role = 5,

    /// <summary>
    /// The access package.
    /// </summary>
    Package = 6,

    /// <summary>
    /// The resource.
    /// </summary>
    Resource = 7,

    /// <summary>
    /// The source system/channel.
    /// </summary>
    Source = 8,

    /// <summary>
    /// The activity type catalog entry.
    /// </summary>
    ActivityType = 9,
}
