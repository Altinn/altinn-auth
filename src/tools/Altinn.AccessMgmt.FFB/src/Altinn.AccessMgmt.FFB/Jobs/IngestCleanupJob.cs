using System.Diagnostics;
using Altinn.AccessMgmt.FFB.Jobs.Models;

namespace Altinn.AccessMgmt.FFB.Jobs;

public static class IngestCleanupJob
{
    public const string JobName = "IngestCleanup";

    public static async Task RunAsync(
        DuoRepo repo,
        JobRun run,
        IngestCleanupOptions opts,
        CancellationToken ct)
    {
        run.AddLog($"Starting ingest cleanup (filter: {opts.Filter ?? "all"}, size: {opts.Size})");

        // ── Phase 1: first pass ─────────────────────────────────────────────
        run.AddLog("Phase 1 — Getting ingest tables (first pass)...");
        var firstRun = (await repo.GetIngestTables(opts.Filter, opts.Size, ct)).ToList();
        run.AddLog($"  Count: {firstRun.Count}");

        // ── Phase 2: wait + second pass ─────────────────────────────────────
        run.AddLog("Waiting 2 minutes before second pass...");
        await Task.Delay(TimeSpan.FromMinutes(2), ct);

        run.AddLog("Phase 2 — Getting ingest tables (second pass)...");
        var secondRun = (await repo.GetIngestTables(opts.Filter, opts.Size, ct)).ToList();
        run.AddLog($"  Count: {secondRun.Count}");

        // ── Intersect ───────────────────────────────────────────────────────
        var intersect = firstRun.Select(t => t.Name)
            .Intersect(secondRun.Select(t => t.Name))
            .ToList();

        run.AddLog($"Stable tables (present in both passes): {intersect.Count}");

        // ── Generate SQL ────────────────────────────────────────────────────
        var statements = intersect.Select(GenerateDropIngestTable).ToList();
        run.AddSql(statements);
        run.AddLog($"Generated {statements.Count} DROP statements.");

        if (opts.Execute)
        {
            run.AddLog($"Execute = true — {opts.Runners} runner(s) will start automatically.");
        }
        else
        {
            run.AddLog("Execute = false — SQL generated for review. Use 'Execute SQL' to run.");
        }
    }

    /// <summary>
    /// Executes the DROP statements for a previously completed preview run.
    /// Called from UI after user confirms via dialog.
    /// </summary>
    public static async Task ExecuteDropsAsync(
        DuoRepo repo,
        IReadOnlyList<string> tableNames,
        JobRun run,
        CancellationToken ct)
    {
        run.AddLog($"Executing {tableNames.Count} DROP statements...");

        var stopwatch = Stopwatch.StartNew();
        var count = tableNames.Count;

        for (var i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var table = tableNames[i];
            await repo.DropIngestTable(table, ct);

            var completed = i + 1;
            if (completed % 500 == 0 || completed == count)
            {
                run.AddLog($"  {completed}/{count} dropped — {stopwatch.ElapsedMilliseconds} ms elapsed");
            }

            if (completed % 10_000 == 0)
            {
                run.AddLog("Pausing 5 seconds...");
                await Task.Delay(5_000, ct);
                stopwatch.Restart();
            }
        }

        run.AddLog($"All {count} tables dropped.");
    }

    private static string GenerateDropIngestTable(string tableName) =>
        $"DROP TABLE IF EXISTS ingest.{tableName} CASCADE;";
}

public record IngestCleanupOptions(
    string? Filter,
    int Size,
    bool Execute,
    int Runners = 1);
