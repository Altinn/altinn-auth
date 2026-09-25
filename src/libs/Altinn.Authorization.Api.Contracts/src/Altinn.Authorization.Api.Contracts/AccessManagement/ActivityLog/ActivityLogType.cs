using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// The main category of the record an activity log entry describes.
/// </summary>
/// <remarks>
/// The numeric values are written directly by the database triggers that populate
/// <c>dbo.activitylog</c> and must not be changed.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLogType
{
    /// <summary>
    /// The entry describes an assignment or one of its child records.
    /// </summary>
    Assignment = 1,

    /// <summary>
    /// The entry describes a delegation or one of its child records.
    /// </summary>
    Delegation = 2,

    /// <summary>
    /// The entry describes a request or one of its child records.
    /// </summary>
    Request = 3,
}
