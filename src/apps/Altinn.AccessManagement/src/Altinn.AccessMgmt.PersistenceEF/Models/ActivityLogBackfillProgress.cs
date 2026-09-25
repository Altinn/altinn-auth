namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Per-source-table progress marker for the one-off job that backfills <c>dbo.activitylog</c>
/// from the live and history tables.
/// </summary>
/// <remarks>
/// Seeded by the migration that installs the activity log triggers, with <see cref="Cutoff"/>
/// set to the moment the triggers went live: the backfill only synthesizes events strictly
/// before the cutoff, so it can never duplicate trigger-written entries.
/// </remarks>
public class ActivityLogBackfillProgress
{
    /// <summary>
    /// The source table name (e.g. <c>assignment</c>, <c>delegationpackage</c>).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The moment the activity log triggers went live; only events before this are backfilled.
    /// </summary>
    public DateTimeOffset Cutoff { get; set; }

    /// <summary>
    /// How far the backfill has come for this source; events before the cursor are done.
    /// </summary>
    public DateTimeOffset? Cursor { get; set; }

    /// <summary>
    /// When the backfill finished for this source.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
