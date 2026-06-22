using System.Text;
using Altinn.AccessMgmt.FFB.Jobs.Models;

namespace Altinn.AccessMgmt.FFB.Jobs;

public static class ResourceCleanupJob
{
    public const string JobName = "ResourceCleanup";

    public static async Task RunAsync(
        DuoRepo repo,
        JobRun run,
        ResourceCleanupOptions opts,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(opts.ResourceRefId))
        {
            run.AddLog("ResourceRefId er tomt — avbryter.", isError: true);
            return;
        }

        run.AddLog($"Søker etter AssignmentResource-rader for resource.refid = '{opts.ResourceRefId}'...");

        // ── Step 1: Find AssignmentResource + Assignment IDs ────────────────
        const string findResourcesSql = """
            SELECT
                ar.id       AS assignmentresourceid,
                a.id        AS assignmentid
            FROM dbo.resource r
            JOIN dbo.assignmentresource ar ON ar.resourceid = r.id
            JOIN dbo."assignment"        a  ON a.id = ar.assignmentid
            WHERE r.refid = @refid
            """;

        var toRemove = await repo.QueryAccAsync<ArRecord>(findResourcesSql, new { refid = opts.ResourceRefId }, ct);

        run.AddLog($"Fant {toRemove.Count} AssignmentResource-rad(er).");

        if (toRemove.Count == 0)
        {
            run.AddLog("Ingenting å slette.");
            return;
        }

        // ── Step 2: Find empty Assignments ──────────────────────────────────
        // An assignment is "empty" after this cleanup if it has no other
        // resources (from other refids), no delegations, no packages, no instances.
        const string emptyAssignmentsSql = """
            SELECT DISTINCT a.id
            FROM dbo."assignment" a
            JOIN dbo.assignmentresource ar ON ar.assignmentid = a.id
            JOIN dbo.resource r            ON r.id = ar.resourceid AND r.refid = @refid
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.delegation d
                WHERE d.toid = a.id OR d.fromid = a.id
            )
            AND NOT EXISTS (
                SELECT 1 FROM dbo.assignmentresource other_ar
                JOIN dbo.resource other_r ON other_r.id = other_ar.resourceid
                WHERE other_ar.assignmentid = a.id AND other_r.refid <> @refid
            )
            AND NOT EXISTS (
                SELECT 1 FROM dbo.assignmentpackage ap WHERE ap.assignmentid = a.id
            )
            AND NOT EXISTS (
                SELECT 1 FROM dbo.assignmentinstance ai WHERE ai.assignmentid = a.id
            )
            """;

        var emptyAssignments = await repo.QueryAccAsync<Guid>(emptyAssignmentsSql, new { refid = opts.ResourceRefId }, ct);

        run.AddLog($"Fant {emptyAssignments.Count} tomme Assignment-rad(er) som kan fjernes.");

        if (opts.SystemAccountId == Guid.Empty)
        {
            run.AddLog("Advarsel: SystemAccountId er Guid.Empty — audit-kontekst vil bruke tom UUID.", isError: true);
        }

        // ── Step 3: Build SQL output for display ────────────────────────────
        var accessRightIds = toRemove.Select(r => r.AssignmentResourceId).ToList();
        var assignmentIds = opts.RemoveEmptyAssignments ? emptyAssignments.ToList() : [];

        var sqlLines = new List<string>
        {
            $"-- DELETE {accessRightIds.Count} AssignmentResource-rader (resource.refid = '{opts.ResourceRefId}')",
            $"DELETE FROM dbo.assignmentresource WHERE id IN ({FormatIds(accessRightIds)});",
        };

        if (opts.RemoveEmptyAssignments && assignmentIds.Count > 0)
        {
            sqlLines.Add($"-- DELETE {assignmentIds.Count} tomme Assignment-rader");
            sqlLines.Add($"DELETE FROM dbo.\"assignment\" WHERE id IN ({FormatIds(assignmentIds)});");
        }
        else if (opts.RemoveEmptyAssignments && assignmentIds.Count == 0)
        {
            sqlLines.Add("-- Ingen tomme Assignment-rader å slette");
        }
        else
        {
            sqlLines.Add("-- RemoveEmptyAssignments = false — Assignment-rader beholdes");
        }

        run.AddSql(sqlLines);
        run.SetDownloadScript(BuildTransactionScript(accessRightIds, assignmentIds, opts, run.Id));

        run.AddLog("Preview klar. Gjennomgå SQL og trykk 'Kjør SQL' for å utføre.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static string FormatIds(IEnumerable<Guid> ids) =>
        string.Join(", ", ids.Select(id => $"'{id}'"));

    private static string BuildTransactionScript(
        IReadOnlyList<Guid> accessRightIds,
        IReadOnlyList<Guid> assignmentIds,
        ResourceCleanupOptions opts,
        Guid operationRunId)
    {
        var operationId = string.IsNullOrWhiteSpace(opts.OperationId)
            ? operationRunId.ToString()
            : opts.OperationId;

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN;");
        sb.AppendLine();
        sb.AppendLine("CREATE TEMP TABLE IF NOT EXISTS session_audit_context (");
        sb.AppendLine("    changed_by           UUID,");
        sb.AppendLine("    changed_by_system     UUID,");
        sb.AppendLine("    change_operation_id   TEXT");
        sb.AppendLine(") ON COMMIT DROP;");
        sb.AppendLine("TRUNCATE session_audit_context;");
        sb.AppendLine($"INSERT INTO session_audit_context VALUES ('{opts.SystemAccountId}', '{opts.SystemAccountId}', '{operationId}');");
        sb.AppendLine();
        sb.AppendLine($"SET LOCAL app.changed_by             = '{opts.SystemAccountId}';");
        sb.AppendLine($"SET LOCAL app.changed_by_system      = '{opts.SystemAccountId}';");
        sb.AppendLine($"SET LOCAL app.change_operation_id    = '{operationId}';");
        sb.AppendLine();
        sb.AppendLine("-- ── Delete AssignmentResource ──────────────────────────────────────");
        sb.AppendLine($"-- resource.refid = '{opts.ResourceRefId}'  ({accessRightIds.Count} rader)");
        sb.AppendLine($"DELETE FROM dbo.assignmentresource");
        sb.AppendLine($"WHERE id IN ({FormatIds(accessRightIds)});");
        sb.AppendLine();

        if (assignmentIds.Count > 0)
        {
            sb.AppendLine("-- ── Delete tomme Assignments ───────────────────────────────────────");
            sb.AppendLine($"-- ({assignmentIds.Count} rader)");
            sb.AppendLine($"DELETE FROM dbo.\"assignment\"");
            sb.AppendLine($"WHERE id IN ({FormatIds(assignmentIds)});");
            sb.AppendLine();
        }

        sb.Append("COMMIT;");
        return sb.ToString();
    }

    private class ArRecord
    {
        public Guid AssignmentResourceId { get; set; }

        public Guid AssignmentId { get; set; }
    }
}

public record ResourceCleanupOptions(
    string ResourceRefId,
    Guid SystemAccountId,
    string OperationId,
    bool RemoveEmptyAssignments);
