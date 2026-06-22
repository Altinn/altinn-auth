using System.Text;
using Altinn.AccessMgmt.FFB.Jobs.Models;

namespace Altinn.AccessMgmt.FFB.Jobs;

public static class AssignmentSyncJob
{
    public const string JobName = "AssignmentSync";

    public static async Task RunAsync(
        DuoRepo repo,
        JobRun run,
        AssignmentSyncOptions opts,
        CancellationToken ct)
    {
        run.AddLog($"Starting run ({run.Id})");

        if (opts.SystemAccountId == Guid.Empty)
        {
            run.AddLog("Warning: SystemAccountId is Guid.Empty — audit context will use empty UUID.", isError: true);
        }

        // ── Phase 1: grouped count diff ────────────────────────────────────
        run.AddLog("Phase 1a — Grouped count (first pass)");
        var firstCntDiff = (await GetAssignmentCountDiff(repo, ct)).ToList();

        run.AddLog("Waiting 120 seconds before second pass...");
        await Task.Delay(TimeSpan.FromSeconds(120), ct);

        run.AddLog("Phase 1b — Grouped count (second pass)");
        var secondCntDiff = (await GetAssignmentCountDiff(repo, ct)).ToList();

        var cntIntersectDiff = firstCntDiff
            .Intersect(secondCntDiff, AssignmentGroupCountKeyComparer.Ordinal)
            .ToList();

        run.AddLog($"Intersected count entries: {cntIntersectDiff.Count}");

        bool needsFullScan = opts.ForceFullScan;
        foreach (var item in cntIntersectDiff)
        {
            if (item.AccCount != item.RegCount)
            {
                needsFullScan = true;
                run.AddLog($"  Diff in {item.RoleCode}: Acc={item.AccCount} Reg={item.RegCount}", isError: true);
            }
        }

        if (!needsFullScan)
        {
            run.AddLog("No grouped assignment diff — done.");
            return;
        }

        run.AddLog(opts.ForceFullScan
            ? "Full scan forced by user."
            : "Grouped diff exists — running full scan.");

        // ── Phase 2: full assignment diff ───────────────────────────────────
        run.AddLog("Phase 2a — Full assignment diff (first pass)...");
        var (firstAccDiff, firstRegDiff) = await GetAssignmentDiff(repo, ct);
        run.AddLog($"  First pass — INSERT: {firstAccDiff.Count}  DELETE: {firstRegDiff.Count}");

        run.AddLog("Waiting 120 seconds before second pass...");
        await Task.Delay(TimeSpan.FromSeconds(120), ct);

        run.AddLog("Phase 2b — Full assignment diff (second pass)...");
        var (secondAccDiff, secondRegDiff) = await GetAssignmentDiff(repo, ct);
        run.AddLog($"  Second pass — INSERT: {secondAccDiff.Count}  DELETE: {secondRegDiff.Count}");

        // Intersect: stable diffs only
        var accIntersect = firstAccDiff.Intersect(secondAccDiff, AssignmentKeyComparer.Ordinal).ToList();
        var regIntersect = firstRegDiff.Intersect(secondRegDiff, AssignmentKeyComparer.Ordinal).ToList();

        run.AddLog($"Intersected — INSERT: {accIntersect.Count}  DELETE: {regIntersect.Count}");

        // ── Phase 3: generate SQL ───────────────────────────────────────────
        run.AddLog("Fetching roles for SQL generation...");
        var roles = (await repo.GetAccRoles(ct)).ToList();

        var statements = new List<string>(accIntersect.Count + regIntersect.Count);

        foreach (var key in accIntersect)
        {
            statements.Add(GenerateInsertAssignment(key, roles));
        }

        foreach (var key in regIntersect)
        {
            statements.Add(GenerateDeleteAssignment(key, roles));
        }

        run.AddSql(statements);
        run.SetDownloadScript(BuildTransactionScript(statements, opts.SystemAccountId, run.Id));
        run.AddLog($"Generated {statements.Count} SQL statements (wrapped in transaction with audit context).");
    }

    private static string BuildTransactionScript(
        IReadOnlyList<string> statements,
        Guid systemAccountId,
        Guid operationId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN TRANSACTION;");
        sb.AppendLine();
        sb.AppendLine("CREATE TEMP TABLE IF NOT EXISTS session_audit_context (");
        sb.AppendLine("    changed_by UUID,");
        sb.AppendLine("    changed_by_system UUID,");
        sb.AppendLine("    change_operation_id TEXT");
        sb.AppendLine(") ON COMMIT DROP;");
        sb.AppendLine("TRUNCATE session_audit_context;");
        sb.AppendLine("INSERT INTO session_audit_context (changed_by, changed_by_system, change_operation_id)");
        sb.AppendLine($"VALUES ('{systemAccountId}', '{systemAccountId}', '{operationId}');");
        sb.AppendLine();
        sb.AppendLine($"SET LOCAL app.changed_by = '{systemAccountId}';");
        sb.AppendLine($"SET LOCAL app.changed_by_system = '{systemAccountId}';");
        sb.AppendLine($"SET LOCAL app.change_operation_id = '{operationId}';");
        sb.AppendLine();
        sb.AppendLine("------");
        sb.AppendLine("-- SCRIPT TO EXECUTE");
        sb.AppendLine("------");

        foreach (var stmt in statements)
        {
            sb.AppendLine(stmt);
        }

        sb.AppendLine();
        sb.Append("COMMIT TRANSACTION;");
        return sb.ToString();
    }

    private static async Task<IEnumerable<AssignmentGroupCountKey>> GetAssignmentCountDiff(
        DuoRepo repo, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var accTask = repo.CountGroupedAccAssignments(ct);
        var regTask = repo.CountGroupedRegAssignments(ct);
        await Task.WhenAll(accTask, regTask);

        var accCnt = accTask.Result.ToList();
        var regCnt = regTask.Result.ToList();

        var result = new List<AssignmentGroupCountKey>();
        foreach (var acc in accCnt)
        {
            var reg = regCnt.FirstOrDefault(r => r.Key == acc.Key);
            result.Add(new AssignmentGroupCountKey(acc.Key, acc.Val, reg?.Val ?? 0));
        }

        foreach (var reg in regCnt.Where(r => !result.Exists(x => x.RoleCode == r.Key)))
        {
            result.Add(new AssignmentGroupCountKey(reg.Key, 0, reg.Val));
        }

        return result;
    }

    private static async Task<(List<AssignmentKey> AccDiff, List<AssignmentKey> RegDiff)>
        GetAssignmentDiff(DuoRepo repo, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var accTask = repo.GetAccAssignments(ct);
        var regTask = repo.GetRegAssignments(ct);
        await Task.WhenAll(accTask, regTask);

        var accSet = new HashSet<AssignmentKey>(
            accTask.Result.Select(a => new AssignmentKey(a.FromId, a.ToId, a.RoleCode)),
            AssignmentKeyComparer.Ordinal);
        var regSet = new HashSet<AssignmentKey>(
            regTask.Result.Select(a => new AssignmentKey(a.FromId, a.ToId, a.RoleCode)),
            AssignmentKeyComparer.Ordinal);

        // In Register but not in AccessMgmt → INSERT
        var accDiff = regSet.Where(k => !accSet.Contains(k)).ToList();

        // In AccessMgmt but not in Register → DELETE
        var regDiff = accSet.Where(k => !regSet.Contains(k)).ToList();

        return (AccDiff: accDiff, RegDiff: regDiff);
    }

    private static string GenerateInsertAssignment(AssignmentKey key, List<JobRole> roles)
    {
        var role = roles.FirstOrDefault(r =>
            string.Equals(r.Code, key.RoleCode, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            return $"-- Role not found for code: {key.RoleCode}";
        }

        return $"INSERT INTO dbo.assignment (id, fromid, toid, roleid) VALUES ('{Guid.CreateVersion7()}','{key.FromId}','{key.ToId}','{role.Id}');";
    }

    private static string GenerateDeleteAssignment(AssignmentKey key, List<JobRole> roles)
    {
        var role = roles.FirstOrDefault(r =>
            string.Equals(r.Code, key.RoleCode, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            return $"-- Role not found for code: {key.RoleCode}";
        }

        return $"DELETE FROM dbo.assignment WHERE fromid = '{key.FromId}' AND toid = '{key.ToId}' AND roleid = '{role.Id}';";
    }
}

public record AssignmentSyncOptions(bool ForceFullScan, Guid SystemAccountId);
