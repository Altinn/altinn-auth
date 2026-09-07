using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// One entry in the activity type catalog: a valid activity log combination with display name
/// and description. The four key fields are the hierarchy: Type, then Subtype (null means the
/// main record itself), then Trigger, then Status. A Status of null is the fallback entry
/// matching any status; entries with a Status override it for that specific status.
/// </summary>
public class ActivityTypeDto
{
    /// <summary>
    /// Unique identifier of the catalog entry, usable as the typeId filter value.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The main record type the entry applies to.
    /// </summary>
    public ActivityLogType Type { get; set; }

    /// <summary>
    /// The child record type, or null for entries about the main record.
    /// </summary>
    public ActivityLogSubtype? Subtype { get; set; }

    /// <summary>
    /// The operation the entry applies to.
    /// </summary>
    public ActivityLogTrigger Trigger { get; set; }

    /// <summary>
    /// The request status the entry applies to, or null for any status.
    /// </summary>
    public RequestStatus? Status { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// One-sentence description of what the event means.
    /// </summary>
    public string Description { get; set; }
}
