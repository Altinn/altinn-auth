using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// The database operation that produced an activity log entry.
/// </summary>
/// <remarks>
/// The numeric values are written directly by the database triggers that populate
/// <c>dbo.activitylog</c> and must not be changed.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLogTrigger
{
    /// <summary>
    /// The record was created.
    /// </summary>
    Created = 1,

    /// <summary>
    /// The record was updated. For request child records this means the status changed;
    /// for assignment instances it means the instance was moved to another assignment.
    /// </summary>
    Updated = 2,

    /// <summary>
    /// The record was deleted, directly or through a cascading delete.
    /// </summary>
    Deleted = 3,
}
