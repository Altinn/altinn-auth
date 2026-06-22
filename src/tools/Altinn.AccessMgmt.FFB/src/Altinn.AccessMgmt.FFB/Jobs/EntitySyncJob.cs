using Altinn.AccessMgmt.FFB.Jobs.Models;

namespace Altinn.AccessMgmt.FFB.Jobs;

public static class EntitySyncJob
{
    public const string JobName = "EntitySync";

    public static async Task RunAsync(
        DuoRepo repo,
        JobRun run,
        CancellationToken ct)
    {
        run.AddLog($"Starting entity sync run ({run.Id})");

        // ── Phase 1: first pass ─────────────────────────────────────────────
        run.AddLog("Phase 1 — Getting party diff (first pass)...");
        var (firstAccDiff, firstRegDiff) = await GetPartyDiff(repo, ct);
        run.AddLog($"  INSERT: {firstAccDiff.Count}  DELETE: {firstRegDiff.Count}");

        if (firstAccDiff.Count < 10)
        {
            foreach (var id in firstAccDiff)
            {
                run.AddLog($"  INSERT candidate: {id}");
            }
        }

        if (firstRegDiff.Count < 10)
        {
            foreach (var id in firstRegDiff)
            {
                run.AddLog($"  DELETE candidate: {id}");
            }
        }

        // ── Phase 2: wait + second pass ─────────────────────────────────────
        run.AddLog("Waiting 60 seconds before second pass...");
        await Task.Delay(TimeSpan.FromSeconds(60), ct);

        run.AddLog("Phase 2 — Getting party diff (second pass)...");
        var (secondAccDiff, secondRegDiff) = await GetPartyDiff(repo, ct);
        run.AddLog($"  INSERT: {secondAccDiff.Count}  DELETE: {secondRegDiff.Count}");

        // ── Intersect ───────────────────────────────────────────────────────
        var accIntersect = firstAccDiff.Intersect(secondAccDiff).ToList();
        var regIntersect = firstRegDiff.Intersect(secondRegDiff).ToList();

        run.AddLog($"Intersected — INSERT: {accIntersect.Count}  DELETE: {regIntersect.Count}");

        // ── Generate SQL ────────────────────────────────────────────────────
        var statements = new List<string>(accIntersect.Count + regIntersect.Count);

        foreach (var id in accIntersect)
        {
            statements.Add(GenerateResyncStatement(id));
        }

        foreach (var id in regIntersect)
        {
            statements.Add($"-- In AccessMgmt but not in Register (manual review needed): {id}");
        }

        run.AddSql(statements);
        run.SetDownloadScript(BuildResyncScript(accIntersect));
        run.AddLog($"Generated {accIntersect.Count} resync statement(s) for Register, {regIntersect.Count} DELETE candidate(s) (manual review).");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static async Task<(List<Guid> AccDiff, List<Guid> RegDiff)> GetPartyDiff(
        DuoRepo repo, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var accTask = repo.GetAccEntity(ct);
        var regTask = repo.GetRegEntity(ct);
        await Task.WhenAll(accTask, regTask);

        var accSet = new HashSet<Guid>(accTask.Result.Select(e => e.Id));
        var regSet = new HashSet<Guid>(regTask.Result.Select(e => e.Id));

        // In Register but not in AccessMgmt → INSERT
        var accDiff = regSet.Where(id => !accSet.Contains(id)).ToList();

        // In AccessMgmt but not in Register → DELETE
        var regDiff = accSet.Where(id => !regSet.Contains(id)).ToList();

        return (AccDiff: accDiff, RegDiff: regDiff);
    }

    private static string GenerateResyncStatement(Guid partyUuid) =>
        $"UPDATE register.party SET version_id = register.tx_nextval('register.party_version_id_seq'::regclass) WHERE \"uuid\" = '{partyUuid}';";

    private static string BuildResyncScript(IReadOnlyList<Guid> partyUuids)
    {
        if (partyUuids.Count == 0)
        {
            return "-- No entities to resync.";
        }

        var lines = new System.Text.StringBuilder();
        lines.AppendLine("BEGIN;");
        lines.AppendLine();
        lines.AppendLine($"-- Resync {partyUuids.Count} party(ies) in Register");
        lines.AppendLine($"-- Runs against Register database");
        lines.AppendLine();
        
        foreach (var id in partyUuids)
        {
            lines.AppendLine(GenerateResyncStatement(id));
        }

        lines.AppendLine();
        lines.Append("COMMIT;");
        return lines.ToString();
    }
}
