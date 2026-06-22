namespace Altinn.AccessMgmt.FFB.Config;

/// <summary>
/// Valid job type identifiers for use in <see cref="JobScheduleEntry.JobType"/>.
/// Using a constant here prevents silent typos in appsettings.json.
/// </summary>
public static class JobTypes
{
    public const string IngestCleanup = "IngestCleanup";
    public const string HistoryCleanup = "HistoryCleanup";
    public const string AssignmentSync = "AssignmentSync";
    public const string EntitySync = "EntitySync";
}

public class JobSchedulesConfig
{
    /// <summary>
    /// Global kill-switch. When false, NO scheduled jobs fire — neither cron nor on-startup.
    /// Defaults to false so scheduling must be explicitly opted in via config.
    /// </summary>
    public bool Enabled { get; set; } = false;

    public List<JobScheduleEntry> Schedules { get; set; } = [];
}

/// <summary>
/// Configures a single scheduled job run. Mutable class (not record) because
/// it is deserialized from JSON config and all setters must be writable.
/// </summary>
public class JobScheduleEntry
{
    /// <summary>Unique identifier used for last-run tracking and display. E.g. "ingest-at23-assignment-4h".</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>One of: <see cref="JobTypes.IngestCleanup"/>, <see cref="JobTypes.HistoryCleanup"/>, <see cref="JobTypes.AssignmentSync"/>, <see cref="JobTypes.EntitySync"/>.</summary>
    public string JobType { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    /// <summary>Standard 5-field cron expression, e.g. "0 */4 * * *" (every 4 hours).</summary>
    public string Cron { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    // ── Per-job options — only the relevant one should be set ─────────────────
    public ScheduledIngestOptions? IngestOptions { get; set; }

    public ScheduledHistoryOptions? HistoryOptions { get; set; }

    public ScheduledAssignmentOptions? AssignmentOptions { get; set; }

    // EntitySync has no options beyond Environment — no options object needed.
}

public class ScheduledIngestOptions
{
    /// <summary>Filter: "assignment", "entity", "provider", or null/empty for all.</summary>
    public string? Filter { get; set; }

    public int Size { get; set; } = 10000;

    /// <summary>When true the execute phase runs automatically after the scan.</summary>
    public bool Execute { get; set; } = false;

    public int Runners { get; set; } = 1;
}

public class ScheduledHistoryOptions
{
    /// <summary>Key from HistoryCleanupJob.KnownTables, e.g. "Provider" or "Resource".</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>When true the execute phase runs automatically after the scan.</summary>
    public bool Execute { get; set; } = false;
}

public class ScheduledAssignmentOptions
{
    public bool ForceFullScan { get; set; } = false;

    /// <summary>UUID of the system account used as changed_by in audit context. Defaults to DBA if empty.</summary>
    public string SystemAccountId { get; set; } = string.Empty;
}
