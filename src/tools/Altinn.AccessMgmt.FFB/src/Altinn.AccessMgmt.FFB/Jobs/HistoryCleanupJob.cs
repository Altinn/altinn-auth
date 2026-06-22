using System.Diagnostics;
using Altinn.AccessMgmt.FFB.Jobs.Models;

namespace Altinn.AccessMgmt.FFB.Jobs;

// ──────────────────────────────────────────────
// Configuration
// ──────────────────────────────────────────────

/// <summary>
/// Describes one history table: where it lives and which columns carry the actual data
/// (as opposed to audit metadata like changedby/operation).
/// </summary>
public sealed record HistoryTableConfig(string TableName, string[] DataColumns);

public record HistoryCleanupOptions(string Table, bool Execute);

// ──────────────────────────────────────────────
// Internal merge plan
// ──────────────────────────────────────────────

/// <summary>
/// One merge operation: the survivor row keeps the oldest validFrom and gets a new validTo;
/// all rows in RowsToDelete are removed.
/// </summary>
public sealed class HistoryMerge
{
    public required object EntityId { get; init; }

    public required IDictionary<string, object> SurvivorRow { get; init; }

    public required object NewValidTo { get; init; }

    public required IReadOnlyList<(object ValidFrom, object ValidTo)> RowsToDelete { get; init; }
}

// ──────────────────────────────────────────────
// Job
// ──────────────────────────────────────────────

public static class HistoryCleanupJob
{
    public const string JobName = "HistoryCleanup";

    /// <summary>
    /// Registry of supported history tables. Add entries here as new tables need cleanup.
    /// DataColumns must be lowercase (PostgreSQL column names) and contain only the
    /// business-data columns — NOT the audit metadata columns (changedby, operation, etc.).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, HistoryTableConfig> KnownTables =
        new Dictionary<string, HistoryTableConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["Provider"] = new(
                "dbo_history.auditprovider",
                ["name", "refid", "logourl", "code", "typeid"]),
            ["Resource"] = new(
                "dbo_history.auditresource",
                ["name", "description", "refid", "providerid", "typeid"]),
        };

    private const int RowLimitPerEntity = 10_000;

    public static async Task RunAsync(
        DuoRepo repo,
        JobRun run,
        HistoryCleanupOptions opts,
        CancellationToken ct)
    {
        if (!KnownTables.TryGetValue(opts.Table, out var config))
        {
            run.AddLog($"Unknown table key '{opts.Table}'. Valid keys: {string.Join(", ", KnownTables.Keys)}", isError: true);
            return;
        }

        run.AddLog($"History cleanup — {config.TableName}");
        run.AddLog($"Comparing data columns: {string.Join(", ", config.DataColumns)}");
        run.AddLog("Note: rows with identical consecutive data values will be merged (oldest validFrom, newest validTo). Zero-duration rows (validFrom = validTo) are deleted.");

        // ── Step 1: fetch distinct entity IDs (small, safe) ─────────────────
        run.AddLog("Fetching distinct entity IDs...");
        var entityIds = await repo.GetHistoryEntityIds(config.TableName, ct);
        run.AddLog($"Found {entityIds.Count} distinct entity IDs. Processing up to {RowLimitPerEntity:N0} rows per entity.");

        ct.ThrowIfCancellationRequested();

        // ── Step 2: iterate entity by entity ─────────────────────────────────
        var allMerges = new List<HistoryMerge>();
        var allZeroDuration = new List<(object Id, object ValidFrom, object ValidTo)>();
        var capped = 0;

        for (var i = 0; i < entityIds.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var entityId = entityIds[i];
            var rows = await repo.GetHistoryRowsForEntity(config.TableName, config.DataColumns, entityId, RowLimitPerEntity, ct);

            if (rows.Count >= RowLimitPerEntity)
            {
                run.AddLog($"  ⚠ {entityId}: hit {RowLimitPerEntity:N0}-row cap — only first {RowLimitPerEntity:N0} rows analysed");
                capped++;
            }

            var groups = rows
                .GroupBy(r => r["id"])
                .Select(g => g.OrderBy(r => (IComparable)r["audit_validfrom"]).ToList())
                .ToList();

            var result = BuildMerges(groups, config.DataColumns);
            allMerges.AddRange(result.Merges);
            allZeroDuration.AddRange(result.ZeroDuration);

            // Progress every 100 entities
            if ((i + 1) % 100 == 0 || i + 1 == entityIds.Count)
            {
                run.AddLog($"  {i + 1}/{entityIds.Count} entities scanned — {allMerges.Sum(m => m.RowsToDelete.Count)} redundant rows found so far");
            }
        }

        if (capped > 0)
        {
            run.AddLog($"Note: {capped} entity/entities hit the {RowLimitPerEntity:N0}-row cap and may need another cleanup pass.");
        }

        // ── Step 3: report + generate SQL ────────────────────────────────────
        var redundantRows = allMerges.Sum(m => m.RowsToDelete.Count);
        run.AddLog($"Duplicate-data rows to remove: {redundantRows} across {allMerges.Count} entities");
        run.AddLog($"Zero-duration rows to remove: {allZeroDuration.Count}");

        if (allMerges.Count == 0 && allZeroDuration.Count == 0)
        {
            run.AddLog("Nothing to clean up.");
            return;
        }

        // Short per-entity summary (max 10 lines)
        foreach (var line in allMerges.Take(10).Select(m => $"  id={m.EntityId}: merged {m.RowsToDelete.Count + 1} → 1 row"))
        {
            run.AddLog(line);
        }

        if (allMerges.Count > 10)
        {
            run.AddLog($"  ... and {allMerges.Count - 10} more entities with duplicates");
        }

        var statements = GenerateSql(allMerges, allZeroDuration, config.TableName);
        run.AddSql(statements);
        run.AddLog($"Generated {statements.Count} SQL statements ({redundantRows + allZeroDuration.Count} DELETEs + {allMerges.Count} UPDATEs).");

        if (!opts.Execute)
        {
            run.AddLog("Execute = false — SQL generated for review. Use 'Execute SQL' to apply.");
            return;
        }

        await ExecuteSqlAsync(repo, statements, run, ct);
    }

    /// <summary>
    /// Executes the SQL statements from a completed preview run.
    /// Called from the UI after the user confirms via dialog.
    /// </summary>
    public static async Task ExecuteSqlAsync(
        DuoRepo repo,
        IReadOnlyList<string> statements,
        JobRun run,
        CancellationToken ct)
    {
        var total = statements.Count;
        run.AddLog($"Executing {total} SQL statements...");

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            await repo.ExecuteSql(statements[i], ct);

            var done = i + 1;
            if (done % 100 == 0 || done == total)
            {
                run.AddLog($"  {done}/{total} — {sw.ElapsedMilliseconds} ms elapsed");
            }
        }

        run.AddLog($"Done. {total} statements executed.");
    }

    // ── Core algorithm ────────────────────────────────────────────────────────
    private static (List<HistoryMerge> Merges, List<(object Id, object ValidFrom, object ValidTo)> ZeroDuration)
        BuildMerges(List<List<IDictionary<string, object>>> groups, string[] dataColumns)
    {
        var merges = new List<HistoryMerge>();
        var zeroDuration = new List<(object Id, object ValidFrom, object ValidTo)>();

        foreach (var group in groups)
        {
            // Collect zero-duration rows first (validFrom == validTo)
            foreach (var row in group)
            {
                if (Equals(row["audit_validfrom"], row["audit_validto"]))
                {
                    zeroDuration.Add((Id: row["id"], ValidFrom: row["audit_validfrom"], ValidTo: row["audit_validto"]));
                }
            }

            // Work on non-zero-duration rows only
            var significant = group
                .Where(r => !Equals(r["audit_validfrom"], r["audit_validto"]))
                .ToList();

            if (significant.Count < 2)
            {
                continue;
            }

            // Walk through sorted rows and collect runs of identical data
            var runStart = 0;
            for (var i = 1; i <= significant.Count; i++)
            {
                var isRunEnd = i == significant.Count
                    || !DataColumnsEqual(significant[i - 1], significant[i], dataColumns);

                if (isRunEnd && i - runStart > 1)
                {
                    // Run from runStart..i-1 has identical data — merge them
                    var toDelete = significant
                        .Skip(runStart + 1)
                        .Take(i - runStart - 1)
                        .Select(r => (ValidFrom: r["audit_validfrom"], ValidTo: r["audit_validto"]))
                        .ToList();

                    merges.Add(new HistoryMerge
                    {
                        EntityId = significant[runStart]["id"],
                        SurvivorRow = significant[runStart],
                        NewValidTo = significant[i - 1]["audit_validto"],
                        RowsToDelete = toDelete,
                    });
                }

                if (isRunEnd)
                {
                    runStart = i;
                }
            }
        }

        return (Merges: merges, ZeroDuration: zeroDuration);
    }

    private static List<string> GenerateSql(
        List<HistoryMerge> merges,
        List<(object Id, object ValidFrom, object ValidTo)> zeroDuration,
        string tableName)
    {
        var statements = new List<string>();

        // Zero-duration deletes first
        foreach (var (id, validFrom, validTo) in zeroDuration)
        {
            statements.Add(
                $"DELETE FROM {tableName} WHERE id = '{id}' AND audit_validfrom = '{FormatTs(validFrom)}' AND audit_validto = '{FormatTs(validTo)}';");
        }

        // Merge operations: DELETE redundant rows, then UPDATE survivor's validto
        foreach (var merge in merges)
        {
            foreach (var (validFrom, validTo) in merge.RowsToDelete)
            {
                statements.Add(
                    $"DELETE FROM {tableName} WHERE id = '{merge.EntityId}' AND audit_validfrom = '{FormatTs(validFrom)}' AND audit_validto = '{FormatTs(validTo)}';");
            }

            statements.Add(
                $"UPDATE {tableName} SET audit_validto = '{FormatTs(merge.NewValidTo)}' WHERE id = '{merge.EntityId}' AND audit_validfrom = '{FormatTs(merge.SurvivorRow["audit_validfrom"])}' AND audit_validto = '{FormatTs(merge.SurvivorRow["audit_validto"])}';");
        }

        return statements;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static bool DataColumnsEqual(
        IDictionary<string, object> a,
        IDictionary<string, object> b,
        string[] columns)
    {
        foreach (var col in columns)
        {
            var va = a.TryGetValue(col, out var av) ? av : null;
            var vb = b.TryGetValue(col, out var bv) ? bv : null;
            if (!Equals(va, vb))
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatTs(object? v) => v switch
    {
        DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff+00"),
        DateTime dt => dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff+00"),
        _ => v?.ToString() ?? "NULL"
    };
}
