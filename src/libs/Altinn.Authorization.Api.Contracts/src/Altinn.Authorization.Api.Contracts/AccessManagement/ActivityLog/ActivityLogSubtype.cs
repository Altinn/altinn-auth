using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// The child record category of an activity log entry. Entries for the main record
/// (assignment, delegation, request) itself carry no subtype.
/// </summary>
/// <remarks>
/// The numeric values are written directly by the database triggers that populate
/// <c>dbo.activitylog</c> and must not be changed.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLogSubtype
{
    /// <summary>
    /// The entry describes an access package child record.
    /// </summary>
    Package = 1,

    /// <summary>
    /// The entry describes a resource child record.
    /// </summary>
    Resource = 2,

    /// <summary>
    /// The entry describes an instance child record.
    /// </summary>
    Instance = 3,
}
