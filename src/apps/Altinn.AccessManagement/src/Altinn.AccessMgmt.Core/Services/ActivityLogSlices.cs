using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Services;

/// <summary>
/// The main record types each activity log area serves — the single source both the per-area
/// service surfaces clamp to and the enduser area declarations build on, so events can never
/// leak between areas even if controller validation regresses.
/// </summary>
public static class ActivityLogSlices
{
    /// <summary>
    /// Assignment and delegation events (the regular connections slice).
    /// </summary>
    public static readonly IReadOnlyList<ActivityLogType> Connections = [ActivityLogType.Assignment, ActivityLogType.Delegation];

    /// <summary>
    /// Access request events.
    /// </summary>
    public static readonly IReadOnlyList<ActivityLogType> Request = [ActivityLogType.Request];

    /// <summary>
    /// Maskinporten schema events are assignment-shaped (main rows and resource children).
    /// </summary>
    public static readonly IReadOnlyList<ActivityLogType> Maskinporten = [ActivityLogType.Assignment];
}
