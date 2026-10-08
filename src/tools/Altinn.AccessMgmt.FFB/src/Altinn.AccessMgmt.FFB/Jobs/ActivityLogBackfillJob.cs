using Altinn.AccessMgmt.FFB.Jobs.Models;
using Dapper;

namespace Altinn.AccessMgmt.FFB.Jobs;

/// <summary>
/// Options for one backfill run.
/// </summary>
/// <param name="Source">A source table name from <see cref="ActivityLogBackfillJob.Sources"/>, or "all".</param>
/// <param name="BatchSize">Rows inserted into dbo.activitylog per batch.</param>
/// <param name="DelayMs">Pause between batches, keeping database load low.</param>
/// <param name="MaxBatches">Total batch budget for the run across all sources; 0 = run until done.</param>
public record ActivityLogBackfillOptions(string Source = "all", int BatchSize = 5000, int DelayMs = 250, int MaxBatches = 0);

/// <summary>
/// Backfills <c>dbo.activitylog</c> with events synthesized from the live tables and their
/// <c>dbo_history</c> audit twins, for everything that happened before the trigger cutoff
/// recorded in <c>dbo.activitylogbackfillprogress</c>.
/// </summary>
/// <remarks>
/// Per source table the job makes one heavy pass that reconstructs each row's version chain
/// (live + history ordered by validfrom): the first version becomes a Created event, a history
/// version whose successor changes a trigger-relevant value becomes an Updated event, and a
/// final history version with no live row becomes a Deleted event — the same semantics as the
/// database triggers. The events land in a session temp table with names resolved as of each
/// event's time through session-local as-of resolvers (see <c>AsOfResolverSql</c>), so
/// backfilled rows carry event-time name snapshots like trigger-written rows, and are then
/// copied into dbo.activitylog in small throttled batches. Only events strictly before the cutoff are staged, and an anti-join on
/// (itemid, trigger, when) skips events that already exist, so the job is idempotent and can be
/// stopped and resumed freely. Progress (latest event time copied) is written back to
/// dbo.activitylogbackfillprogress after every batch, and completedat marks a finished source.
///
/// Known limitation: Updated events are attributed to the successor version's audit_changedby.
/// That is the actual updater for EF-written updates, but raw-SQL updaters (FFB reparenting,
/// legacy scripts) do not refresh the audit columns, so for those versions the history only
/// knows the original creator — the true updater identity was never persisted and cannot be
/// backfilled. Version chains are ordered by (audit_validfrom, audit_validto) for the same
/// reason: raw-SQL updates leave audit_validfrom unchanged across versions — and the as-of
/// name resolution inherits the same imprecision for rows whose history was written that way.
/// </remarks>
public static class ActivityLogBackfillJob
{
    public const string JobName = "ActivityLogBackfill";

    /// <summary>
    /// The source tables, in backfill order.
    /// </summary>
    public static readonly IReadOnlyList<string> Sources =
    [
        "assignment",
        "assignmentpackage",
        "assignmentresource",
        "assignmentinstance",
        "delegation",
        "delegationpackage",
        "delegationresource",
        "requestassignment",
        "requestassignmentpackage",
        "requestassignmentresource",
    ];

    public static async Task RunAsync(DuoRepo repo, JobRun run, ActivityLogBackfillOptions opts, CancellationToken ct)
    {
        var sources = string.Equals(opts.Source, "all", StringComparison.OrdinalIgnoreCase)
            ? Sources
            : Sources.Where(s => string.Equals(s, opts.Source, StringComparison.OrdinalIgnoreCase)).ToList();

        if (sources.Count == 0)
        {
            run.AddLog($"Unknown source '{opts.Source}'. Valid: all, {string.Join(", ", Sources)}", isError: true);
            return;
        }

        var batchSize = Math.Clamp(opts.BatchSize, 100, 100_000);
        var budget = opts.MaxBatches <= 0 ? int.MaxValue : opts.MaxBatches;
        run.AddLog($"Activity log backfill — sources: {string.Join(", ", sources)}; batch size {batchSize}, delay {opts.DelayMs} ms, batch budget {(opts.MaxBatches <= 0 ? "unlimited" : opts.MaxBatches.ToString())}.");

        foreach (var source in sources)
        {
            ct.ThrowIfCancellationRequested();

            if (budget <= 0)
            {
                run.AddLog("Batch budget exhausted — stopping. Run the job again to continue.");
                return;
            }

            budget -= await BackfillSourceAsync(repo, run, source, batchSize, opts.DelayMs, budget, ct);
        }

        run.AddLog("Backfill run finished.");
    }

    /// <summary>
    /// Read-only dry run: counts what a backfill run would insert per source and trigger,
    /// using the exact same version-chain semantics, cutoff and anti-join as the real job.
    /// Writes nothing — no staging table, no progress updates.
    /// </summary>
    public static async Task AnalyzeAsync(DuoRepo repo, JobRun run, string source, CancellationToken ct)
    {
        var sources = string.Equals(source, "all", StringComparison.OrdinalIgnoreCase)
            ? Sources
            : Sources.Where(s => string.Equals(s, source, StringComparison.OrdinalIgnoreCase)).ToList();

        if (sources.Count == 0)
        {
            run.AddLog($"Unknown source '{source}'. Valid: all, {string.Join(", ", Sources)}", isError: true);
            return;
        }

        run.AddLog($"Activity log backfill analysis — sources: {string.Join(", ", sources)}. Counts what a backfill run would insert; nothing is written.");

        long grandTotal = 0;

        foreach (var s in sources)
        {
            ct.ThrowIfCancellationRequested();
            grandTotal += await AnalyzeSourceAsync(repo, run, s, ct);
        }

        run.AddLog($"Analysis finished — {grandTotal:N0} events would be inserted in total.");
    }

    private static async Task<long> AnalyzeSourceAsync(DuoRepo repo, JobRun run, string source, CancellationToken ct)
    {
        await using var conn = repo.CreateAccConnection();
        await conn.OpenAsync(ct);

        var hasProgressTable = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT to_regclass('dbo.activitylogbackfillprogress') IS NOT NULL;", cancellationToken: ct));
        var hasLogTable = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT to_regclass('dbo.activitylog') IS NOT NULL;", cancellationToken: ct));

        DateTime cutoff;
        string cutoffNote;

        if (hasProgressTable)
        {
            var progress = await conn.QuerySingleOrDefaultAsync<(DateTime Cutoff, DateTime? Cursor, DateTime? CompletedAt)>(new CommandDefinition(
                "SELECT cutoff, \"cursor\", completedat FROM dbo.activitylogbackfillprogress WHERE source = @source;",
                new { source },
                cancellationToken: ct));

            if (progress == default)
            {
                run.AddLog($"[{source}] no progress row found — was the ActivityLog migration applied?", isError: true);
                return 0;
            }

            cutoff = progress.Cutoff;
            cutoffNote = progress.CompletedAt is not null ? $"; marked completed {progress.CompletedAt:u}" : string.Empty;
        }
        else
        {
            cutoff = DateTime.UtcNow;
            cutoffNote = "; schema not installed — counting everything up to now";
        }

        var perTrigger = (await conn.QueryAsync<(int Trigger, long Count)>(new CommandDefinition(
            AnalyzeSql(source, includeAntiJoin: hasLogTable),
            new { cutoff },
            commandTimeout: 0,
            cancellationToken: ct))).ToDictionary(t => t.Trigger, t => t.Count);

        var created = perTrigger.GetValueOrDefault(1);
        var updated = perTrigger.GetValueOrDefault(2);
        var deleted = perTrigger.GetValueOrDefault(3);
        var total = created + updated + deleted;

        run.AddLog($"[{source}] {total:N0} events would be inserted — Created {created:N0}, Updated {updated:N0}, Deleted {deleted:N0} (cutoff {cutoff:u}{cutoffNote}).");
        return total;
    }

    private static async Task<int> BackfillSourceAsync(DuoRepo repo, JobRun run, string source, int batchSize, int delayMs, int budget, CancellationToken ct)
    {
        await using var conn = repo.CreateAccConnection();
        await conn.OpenAsync(ct);

        // Npgsql returns timestamptz as UTC DateTime; Dapper maps the positional tuple by column type.
        var progress = await conn.QuerySingleOrDefaultAsync<(DateTime Cutoff, DateTime? Cursor, DateTime? CompletedAt)>(new CommandDefinition(
            "SELECT cutoff, \"cursor\", completedat FROM dbo.activitylogbackfillprogress WHERE source = @source;",
            new { source },
            cancellationToken: ct));

        if (progress == default)
        {
            run.AddLog($"[{source}] no progress row found — was the ActivityLog migration applied?", isError: true);
            return 0;
        }

        if (progress.CompletedAt is not null)
        {
            run.AddLog($"[{source}] already completed at {progress.CompletedAt:u} — skipping.");
            return 0;
        }

        run.AddLog($"[{source}] staging events before cutoff {progress.Cutoff:u} (resuming from {progress.Cursor?.ToString("u") ?? "start"})...");

        await conn.ExecuteAsync(new CommandDefinition("DROP TABLE IF EXISTS alstage;", commandTimeout: 0, cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(StageSql(source), new { cutoff = progress.Cutoff }, commandTimeout: 0, cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition("CREATE INDEX ON alstage (rn);", commandTimeout: 0, cancellationToken: ct));

        var total = await conn.ExecuteScalarAsync<long>(new CommandDefinition("SELECT count(*) FROM alstage;", commandTimeout: 0, cancellationToken: ct));
        run.AddLog($"[{source}] {total:N0} events to insert.");

        var lastRn = 0L;
        var inserted = 0L;
        var batches = 0;

        while (batches < budget)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await conn.QuerySingleAsync<(long? MaxRn, DateTime? MaxWhen)>(new CommandDefinition(
                "SELECT max(rn), max(ev_when) FROM (SELECT rn, ev_when FROM alstage WHERE rn > @lastRn ORDER BY rn LIMIT @batchSize) b;",
                new { lastRn, batchSize },
                commandTimeout: 0,
                cancellationToken: ct));

            if (batch.MaxRn is null)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.activitylogbackfillprogress SET \"cursor\" = cutoff, completedat = now() WHERE source = @source;",
                    new { source },
                    cancellationToken: ct));
                run.AddLog($"[{source}] done — {inserted:N0} events inserted in {batches} batch(es).");
                break;
            }

            var count = await conn.ExecuteAsync(new CommandDefinition(
                InsertBatchSql,
                new { lastRn, upto = batch.MaxRn.Value },
                commandTimeout: 0,
                cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.activitylogbackfillprogress SET \"cursor\" = @cursor WHERE source = @source;",
                new { cursor = batch.MaxWhen, source },
                cancellationToken: ct));

            lastRn = batch.MaxRn.Value;
            inserted += count;
            batches++;

            if (batches % 20 == 0)
            {
                run.AddLog($"[{source}] {inserted:N0}/{total:N0} events inserted ({batches} batches)...");
            }

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, ct);
            }
        }

        if (batches >= budget && lastRn > 0)
        {
            run.AddLog($"[{source}] paused after {batches} batch(es) ({inserted:N0}/{total:N0} events) — budget spent.");
        }

        await conn.ExecuteAsync(new CommandDefinition("DROP TABLE IF EXISTS alstage;", commandTimeout: 0, cancellationToken: ct));
        return batches;
    }

    private const string InsertBatchSql = """
        INSERT INTO dbo.activitylog (
            "type", subtype, "trigger", status, "when", byid, byname, sourceid, operationid,
            fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
            roleid, rolename, viaroleid, viarolename, packageid, packagename,
            resourceid, resourcename, instanceid, itemid, parentid, details)
        SELECT
            type, subtype, ev_trigger, status, ev_when, byid, byname, sourceid, operationid,
            fromid, fromname, fromtype, toid, toname, totype, viaid, vianame, viatype,
            roleid, rolename, viaroleid, viarolename, packageid, packagename,
            resourceid, resourcename, instanceid, itemid, parentid, details
        FROM alstage
        WHERE rn > @lastRn AND rn <= @upto;
        """;

    /// <summary>
    /// Builds the CREATE TEMP TABLE statement that stages every backfill event for one source
    /// table. The event branches mirror the trigger mappings in ActivityLogTriggerScripts.
    /// </summary>
    public static string StageSql(string source)
    {
        var (table, dataColumns, leadColumns, eventBranches) = Parts(source);
        return Stage(table, dataColumns, leadColumns, eventBranches);
    }

    /// <summary>
    /// Builds the read-only count statement for one source table: the same version chain,
    /// cutoff and event conditions as <see cref="StageSql"/>, grouped per trigger. It references
    /// none of the activitylog helper functions, so it also runs in environments where the
    /// ActivityLog migration has not been applied; the anti-join against existing entries is
    /// included only when dbo.activitylog exists.
    /// </summary>
    public static string AnalyzeSql(string source, bool includeAntiJoin = true)
    {
        var (table, dataColumns, leadColumns, _) = Parts(source);
        var updatedCondition = UpdatedCondition(source);

        var updatedBranch = updatedCondition is null ? string.Empty : $"""

                UNION ALL
                SELECT 2 AS ev_trigger, c.audit_validto AS ev_when, c.id AS itemid FROM chain c WHERE c.verno < c.vercount AND ({updatedCondition})
            """;

        var antiJoin = includeAntiJoin ? """

              AND NOT EXISTS (
                  SELECT 1 FROM dbo.activitylog al
                  WHERE al.itemid = e.itemid AND al."trigger" = e.ev_trigger AND al."when" = e.ev_when)
            """ : string.Empty;

        return $"""
            {VersionChain(table, dataColumns, leadColumns)},
            events AS (
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when, c.id AS itemid FROM chain c WHERE c.verno = 1{updatedBranch}
                UNION ALL
                SELECT 3 AS ev_trigger, c.audit_validto AS ev_when, c.id AS itemid FROM chain c WHERE c.verno = c.vercount AND NOT c.is_live
            )
            SELECT e.ev_trigger AS trigger, count(*) AS count
            FROM events e
            WHERE e.ev_when < @cutoff{antiJoin}
            GROUP BY e.ev_trigger;
            """;
    }

    /// <summary>
    /// The per-source condition that makes a version transition an Updated event — must mirror
    /// the Updated branch WHERE clauses in <see cref="Parts"/>.
    /// </summary>
    private static string? UpdatedCondition(string source) => source switch
    {
        "assignmentinstance" => "c.next_assignmentid IS DISTINCT FROM c.assignmentid",
        "requestassignmentpackage" or "requestassignmentresource" => "c.next_status IS DISTINCT FROM c.status",
        _ => null,
    };

    private static (string Table, string DataColumns, string LeadColumns, string EventBranches) Parts(string source) => source switch
    {
        "assignment" => (
            Table: "assignment",
            DataColumns: "id, fromid, toid, roleid",
            LeadColumns: string.Empty,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       1 AS type, NULL::int AS subtype, NULL::int AS status,
                       c.fromid AS fromid, c.toid AS toid, NULL::uuid AS viaid, c.roleid AS roleid, NULL::uuid AS viaroleid,
                       NULL::uuid AS packageid, NULL::uuid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, NULL::uuid AS parentid, NULL::jsonb AS details
                FROM chain c
                WHERE c.verno = 1
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       1, NULL::int, NULL::int,
                       c.fromid, c.toid, NULL::uuid, c.roleid, NULL::uuid,
                       NULL::uuid, NULL::uuid, NULL::text,
                       c.id, NULL::uuid, NULL::jsonb
                FROM chain c
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "assignmentpackage" => (
            Table: "assignmentpackage",
            DataColumns: "id, assignmentid, packageid",
            LeadColumns: string.Empty,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       1 AS type, 1 AS subtype, NULL::int AS status,
                       a.o_fromid AS fromid, a.o_toid AS toid, NULL::uuid AS viaid, a.o_roleid AS roleid, NULL::uuid AS viaroleid,
                       c.packageid AS packageid, NULL::uuid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, c.assignmentid AS parentid, NULL::jsonb AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.assignmentid) a
                WHERE c.verno = 1
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       1, 1, NULL::int,
                       a.o_fromid, a.o_toid, NULL::uuid, a.o_roleid, NULL::uuid,
                       c.packageid, NULL::uuid, NULL::text,
                       c.id, c.assignmentid, NULL::jsonb
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.assignmentid) a
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "assignmentresource" => (
            Table: "assignmentresource",
            DataColumns: "id, assignmentid, resourceid",
            LeadColumns: string.Empty,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       1 AS type, 2 AS subtype, NULL::int AS status,
                       a.o_fromid AS fromid, a.o_toid AS toid, NULL::uuid AS viaid, a.o_roleid AS roleid, NULL::uuid AS viaroleid,
                       NULL::uuid AS packageid, c.resourceid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, c.assignmentid AS parentid, NULL::jsonb AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.assignmentid) a
                WHERE c.verno = 1
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       1, 2, NULL::int,
                       a.o_fromid, a.o_toid, NULL::uuid, a.o_roleid, NULL::uuid,
                       NULL::uuid, c.resourceid, NULL::text,
                       c.id, c.assignmentid, NULL::jsonb
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.assignmentid) a
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "assignmentinstance" => (
            Table: "assignmentinstance",
            DataColumns: "id, assignmentid, resourceid, instanceid",
            LeadColumns: """
                ,
                       lead(assignmentid) OVER w AS next_assignmentid,
                       lead(audit_changedby) OVER w AS next_changedby,
                       lead(audit_changedbysystem) OVER w AS next_changedbysystem,
                       lead(audit_changeoperation) OVER w AS next_changeoperation
                """,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       1 AS type, 3 AS subtype, NULL::int AS status,
                       a.o_fromid AS fromid, a.o_toid AS toid, NULL::uuid AS viaid, a.o_roleid AS roleid, NULL::uuid AS viaroleid,
                       NULL::uuid AS packageid, c.resourceid AS resourceid, c.instanceid AS instanceid,
                       c.id AS itemid, c.assignmentid AS parentid, NULL::jsonb AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.assignmentid) a
                WHERE c.verno = 1
                UNION ALL
                SELECT 2, c.audit_validto,
                       c.next_changedby, c.next_changedbysystem, c.next_changeoperation,
                       1, 3, NULL::int,
                       a.o_fromid, a.o_toid, NULL::uuid, a.o_roleid, NULL::uuid,
                       NULL::uuid, c.resourceid, c.instanceid,
                       c.id, c.next_assignmentid,
                       jsonb_build_object('previousAssignmentId', c.assignmentid)
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.next_assignmentid) a
                WHERE c.verno < c.vercount AND c.next_assignmentid IS DISTINCT FROM c.assignmentid
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       1, 3, NULL::int,
                       a.o_fromid, a.o_toid, NULL::uuid, a.o_roleid, NULL::uuid,
                       NULL::uuid, c.resourceid, c.instanceid,
                       c.id, c.assignmentid, NULL::jsonb
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.assignmentid) a
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "delegation" => (
            Table: "delegation",
            DataColumns: "id, fromid, toid, facilitatorid",
            LeadColumns: string.Empty,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       2 AS type, NULL::int AS subtype, NULL::int AS status,
                       fa.o_fromid AS fromid, ta.o_toid AS toid, c.facilitatorid AS viaid, fa.o_roleid AS roleid, ta.o_roleid AS viaroleid,
                       NULL::uuid AS packageid, NULL::uuid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, NULL::uuid AS parentid, NULL::jsonb AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.fromid) fa
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.toid) ta
                WHERE c.verno = 1
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       2, NULL::int, NULL::int,
                       fa.o_fromid, ta.o_toid, c.facilitatorid, fa.o_roleid, ta.o_roleid,
                       NULL::uuid, NULL::uuid, NULL::text,
                       c.id, NULL::uuid, NULL::jsonb
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.fromid) fa
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(c.toid) ta
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "delegationpackage" => (
            Table: "delegationpackage",
            DataColumns: "id, delegationid, packageid, rolepackageid, assignmentpackageid",
            LeadColumns: string.Empty,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       2 AS type, 1 AS subtype, NULL::int AS status,
                       fa.o_fromid AS fromid, ta.o_toid AS toid, d.o_facilitatorid AS viaid, fa.o_roleid AS roleid, ta.o_roleid AS viaroleid,
                       c.packageid AS packageid, NULL::uuid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, c.delegationid AS parentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('rolePackageId', c.rolepackageid, 'assignmentPackageId', c.assignmentpackageid)), '{}'::jsonb) AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_delegation_info(c.delegationid) d
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_fromassignmentid) fa
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_toassignmentid) ta
                WHERE c.verno = 1
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       2, 1, NULL::int,
                       fa.o_fromid, ta.o_toid, d.o_facilitatorid, fa.o_roleid, ta.o_roleid,
                       c.packageid, NULL::uuid, NULL::text,
                       c.id, c.delegationid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('rolePackageId', c.rolepackageid, 'assignmentPackageId', c.assignmentpackageid)), '{}'::jsonb)
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_delegation_info(c.delegationid) d
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_fromassignmentid) fa
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_toassignmentid) ta
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "delegationresource" => (
            Table: "delegationresource",
            DataColumns: "id, delegationid, resourceid, assignmentresourceid",
            LeadColumns: string.Empty,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       2 AS type, 2 AS subtype, NULL::int AS status,
                       fa.o_fromid AS fromid, ta.o_toid AS toid, d.o_facilitatorid AS viaid, fa.o_roleid AS roleid, ta.o_roleid AS viaroleid,
                       NULL::uuid AS packageid, c.resourceid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, c.delegationid AS parentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('assignmentResourceId', c.assignmentresourceid)), '{}'::jsonb) AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_delegation_info(c.delegationid) d
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_fromassignmentid) fa
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_toassignmentid) ta
                WHERE c.verno = 1
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       2, 2, NULL::int,
                       fa.o_fromid, ta.o_toid, d.o_facilitatorid, fa.o_roleid, ta.o_roleid,
                       NULL::uuid, c.resourceid, NULL::text,
                       c.id, c.delegationid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('assignmentResourceId', c.assignmentresourceid)), '{}'::jsonb)
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_delegation_info(c.delegationid) d
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_fromassignmentid) fa
                CROSS JOIN LATERAL dbo.activitylog_assignment_info(d.o_toassignmentid) ta
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "requestassignment" => (
            Table: "requestassignment",
            DataColumns: "id, fromid, toid, roleid, byid AS requestedbyid",
            LeadColumns: string.Empty,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       3 AS type, NULL::int AS subtype, NULL::int AS status,
                       c.fromid AS fromid, c.toid AS toid, NULL::uuid AS viaid, c.roleid AS roleid, NULL::uuid AS viaroleid,
                       NULL::uuid AS packageid, NULL::uuid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, NULL::uuid AS parentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', c.requestedbyid)), '{}'::jsonb) AS details
                FROM chain c
                WHERE c.verno = 1
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       3, NULL::int, NULL::int,
                       c.fromid, c.toid, NULL::uuid, c.roleid, NULL::uuid,
                       NULL::uuid, NULL::uuid, NULL::text,
                       c.id, NULL::uuid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', c.requestedbyid)), '{}'::jsonb)
                FROM chain c
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "requestassignmentpackage" => (
            Table: "requestassignmentpackage",
            DataColumns: "id, assignmentid, packageid, status",
            LeadColumns: """
                ,
                       lead(status) OVER w AS next_status,
                       lead(audit_changedby) OVER w AS next_changedby,
                       lead(audit_changedbysystem) OVER w AS next_changedbysystem,
                       lead(audit_changeoperation) OVER w AS next_changeoperation
                """,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       3 AS type, 1 AS subtype, c.status AS status,
                       ra.o_fromid AS fromid, ra.o_toid AS toid, NULL::uuid AS viaid, ra.o_roleid AS roleid, NULL::uuid AS viaroleid,
                       c.packageid AS packageid, NULL::uuid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, c.assignmentid AS parentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', ra.o_byid)), '{}'::jsonb) AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_requestassignment_info(c.assignmentid) ra
                WHERE c.verno = 1
                UNION ALL
                SELECT 2, c.audit_validto,
                       c.next_changedby, c.next_changedbysystem, c.next_changeoperation,
                       3, 1, c.next_status,
                       ra.o_fromid, ra.o_toid, NULL::uuid, ra.o_roleid, NULL::uuid,
                       c.packageid, NULL::uuid, NULL::text,
                       c.id, c.assignmentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('previousStatus', c.status, 'requestedById', ra.o_byid)), '{}'::jsonb)
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_requestassignment_info(c.assignmentid) ra
                WHERE c.verno < c.vercount AND c.next_status IS DISTINCT FROM c.status
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       3, 1, c.status,
                       ra.o_fromid, ra.o_toid, NULL::uuid, ra.o_roleid, NULL::uuid,
                       c.packageid, NULL::uuid, NULL::text,
                       c.id, c.assignmentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('requestedById', ra.o_byid)), '{}'::jsonb)
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_requestassignment_info(c.assignmentid) ra
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        "requestassignmentresource" => (
            Table: "requestassignmentresource",
            DataColumns: "id, assignmentid, resourceid, action, status",
            LeadColumns: """
                ,
                       lead(status) OVER w AS next_status,
                       lead(audit_changedby) OVER w AS next_changedby,
                       lead(audit_changedbysystem) OVER w AS next_changedbysystem,
                       lead(audit_changeoperation) OVER w AS next_changeoperation
                """,
            EventBranches: """
                SELECT 1 AS ev_trigger, c.audit_validfrom AS ev_when,
                       c.audit_changedby AS byid, c.audit_changedbysystem AS sourceid, c.audit_changeoperation AS operationid,
                       3 AS type, 2 AS subtype, c.status AS status,
                       ra.o_fromid AS fromid, ra.o_toid AS toid, NULL::uuid AS viaid, ra.o_roleid AS roleid, NULL::uuid AS viaroleid,
                       NULL::uuid AS packageid, c.resourceid AS resourceid, NULL::text AS instanceid,
                       c.id AS itemid, c.assignmentid AS parentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('action', c.action, 'requestedById', ra.o_byid)), '{}'::jsonb) AS details
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_requestassignment_info(c.assignmentid) ra
                WHERE c.verno = 1
                UNION ALL
                SELECT 2, c.audit_validto,
                       c.next_changedby, c.next_changedbysystem, c.next_changeoperation,
                       3, 2, c.next_status,
                       ra.o_fromid, ra.o_toid, NULL::uuid, ra.o_roleid, NULL::uuid,
                       NULL::uuid, c.resourceid, NULL::text,
                       c.id, c.assignmentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('previousStatus', c.status, 'action', c.action, 'requestedById', ra.o_byid)), '{}'::jsonb)
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_requestassignment_info(c.assignmentid) ra
                WHERE c.verno < c.vercount AND c.next_status IS DISTINCT FROM c.status
                UNION ALL
                SELECT 3, c.audit_validto,
                       c.audit_deletedby, c.audit_deletedbysystem, c.audit_deleteoperation,
                       3, 2, c.status,
                       ra.o_fromid, ra.o_toid, NULL::uuid, ra.o_roleid, NULL::uuid,
                       NULL::uuid, c.resourceid, NULL::text,
                       c.id, c.assignmentid,
                       NULLIF(jsonb_strip_nulls(jsonb_build_object('action', c.action, 'requestedById', ra.o_byid)), '{}'::jsonb)
                FROM chain c
                CROSS JOIN LATERAL dbo.activitylog_requestassignment_info(c.assignmentid) ra
                WHERE c.verno = c.vercount AND NOT c.is_live
                """),

        _ => throw new ArgumentException($"Unknown activity log backfill source '{source}'.", nameof(source)),
    };

    private static string VersionChain(string table, string dataColumns, string leadColumns) => $"""
        WITH versions AS (
            SELECT {dataColumns}, audit_validfrom, NULL::timestamptz AS audit_validto,
                   audit_changedby, audit_changedbysystem, audit_changeoperation,
                   NULL::uuid AS audit_deletedby, NULL::uuid AS audit_deletedbysystem, NULL::text AS audit_deleteoperation,
                   true AS is_live
            FROM dbo.{table}
            UNION ALL
            SELECT {dataColumns}, audit_validfrom, audit_validto,
                   audit_changedby, audit_changedbysystem, audit_changeoperation,
                   audit_deletedby, audit_deletedbysystem, audit_deleteoperation,
                   false AS is_live
            FROM dbo_history.audit{table}
        ),
        chain AS (
            SELECT v.*,
                   row_number() OVER w AS verno,
                   count(*) OVER (PARTITION BY id) AS vercount{leadColumns}
            FROM versions v
            WINDOW w AS (PARTITION BY id ORDER BY audit_validfrom, audit_validto NULLS LAST, is_live)
        )
        """;

    private static string Stage(string table, string dataColumns, string leadColumns, string eventBranches) => $"""
        {AsOfResolverSql}
        CREATE TEMP TABLE alstage AS
        {VersionChain(table, dataColumns, leadColumns)},
        events AS (
        {eventBranches}
        )
        SELECT row_number() OVER (ORDER BY e.ev_when, e.itemid, e.ev_trigger) AS rn,
               e.ev_trigger, e.ev_when, e.byid,
               pg_temp.al_entity_name_at(e.byid, e.ev_when) AS byname,
               e.sourceid, e.operationid, e.type, e.subtype, e.status,
               e.fromid, f.o_name AS fromname, f.o_type AS fromtype,
               e.toid, t.o_name AS toname, t.o_type AS totype,
               e.viaid, via.o_name AS vianame, via.o_type AS viatype,
               e.roleid, pg_temp.al_role_name_at(e.roleid, e.ev_when) AS rolename,
               e.viaroleid, pg_temp.al_role_name_at(e.viaroleid, e.ev_when) AS viarolename,
               e.packageid, pg_temp.al_package_name_at(e.packageid, e.ev_when) AS packagename,
               e.resourceid, pg_temp.al_resource_name_at(e.resourceid, e.ev_when) AS resourcename,
               e.instanceid, e.itemid, e.parentid, e.details
        FROM events e
        CROSS JOIN LATERAL pg_temp.al_entity_info_at(e.fromid, e.ev_when) f
        CROSS JOIN LATERAL pg_temp.al_entity_info_at(e.toid, e.ev_when) t
        CROSS JOIN LATERAL pg_temp.al_entity_info_at(e.viaid, e.ev_when) via
        WHERE e.ev_when < @cutoff
          AND NOT EXISTS (
              SELECT 1 FROM dbo.activitylog al
              WHERE al.itemid = e.itemid AND al."trigger" = e.ev_trigger AND al."when" = e.ev_when);
        """;

    // Session-local as-of name resolvers for staging: the version with the greatest
    // audit_validfrom at or before the event time wins (live row or its history twin); when
    // every version starts after the event (rows written later, raw-SQL audit quirks), the
    // earliest version is used. pg_temp on purpose: the dbo.activitylog_* resolvers stay
    // timestamp-free because the triggers run inside the changing transaction, where the live
    // row already is the event-time state.
    private const string AsOfResolverSql = """
        CREATE OR REPLACE FUNCTION pg_temp.al_entity_info_at(p_id uuid, p_at timestamptz, OUT o_name text, OUT o_type text)
        LANGUAGE plpgsql STABLE AS $fn$
        BEGIN
            IF p_id IS NULL THEN
                RETURN;
            END IF;

            SELECT v.name, et.name INTO o_name, o_type
            FROM (
                SELECT e.name, e.typeid, e.audit_validfrom FROM dbo.entity e WHERE e.id = p_id
                UNION ALL
                SELECT h.name, h.typeid, h.audit_validfrom FROM dbo_history.auditentity h WHERE h.id = p_id
            ) v
            LEFT JOIN dbo.entitytype et ON et.id = v.typeid
            ORDER BY (v.audit_validfrom <= p_at) DESC,
                     CASE WHEN v.audit_validfrom <= p_at THEN v.audit_validfrom END DESC,
                     v.audit_validfrom ASC
            LIMIT 1;
        END;
        $fn$;

        CREATE OR REPLACE FUNCTION pg_temp.al_entity_name_at(p_id uuid, p_at timestamptz)
        RETURNS text LANGUAGE sql STABLE STRICT AS $fn$
            SELECT (pg_temp.al_entity_info_at(p_id, p_at)).o_name;
        $fn$;

        CREATE OR REPLACE FUNCTION pg_temp.al_role_name_at(p_id uuid, p_at timestamptz)
        RETURNS text LANGUAGE sql STABLE STRICT AS $fn$
            SELECT v.name FROM (
                SELECT r.name, r.audit_validfrom FROM dbo.role r WHERE r.id = p_id
                UNION ALL
                SELECT h.name, h.audit_validfrom FROM dbo_history.auditrole h WHERE h.id = p_id
            ) v
            ORDER BY (v.audit_validfrom <= p_at) DESC,
                     CASE WHEN v.audit_validfrom <= p_at THEN v.audit_validfrom END DESC,
                     v.audit_validfrom ASC
            LIMIT 1;
        $fn$;

        CREATE OR REPLACE FUNCTION pg_temp.al_package_name_at(p_id uuid, p_at timestamptz)
        RETURNS text LANGUAGE sql STABLE STRICT AS $fn$
            SELECT v.name FROM (
                SELECT p.name, p.audit_validfrom FROM dbo.package p WHERE p.id = p_id
                UNION ALL
                SELECT h.name, h.audit_validfrom FROM dbo_history.auditpackage h WHERE h.id = p_id
            ) v
            ORDER BY (v.audit_validfrom <= p_at) DESC,
                     CASE WHEN v.audit_validfrom <= p_at THEN v.audit_validfrom END DESC,
                     v.audit_validfrom ASC
            LIMIT 1;
        $fn$;

        CREATE OR REPLACE FUNCTION pg_temp.al_resource_name_at(p_id uuid, p_at timestamptz)
        RETURNS text LANGUAGE sql STABLE STRICT AS $fn$
            SELECT v.name FROM (
                SELECT r.name, r.audit_validfrom FROM dbo.resource r WHERE r.id = p_id
                UNION ALL
                SELECT h.name, h.audit_validfrom FROM dbo_history.auditresource h WHERE h.id = p_id
            ) v
            ORDER BY (v.audit_validfrom <= p_at) DESC,
                     CASE WHEN v.audit_validfrom <= p_at THEN v.audit_validfrom END DESC,
                     v.audit_validfrom ASC
            LIMIT 1;
        $fn$;
        """;
}
