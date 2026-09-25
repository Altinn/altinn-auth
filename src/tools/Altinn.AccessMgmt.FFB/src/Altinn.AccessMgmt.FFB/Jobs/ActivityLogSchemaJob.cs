using Altinn.AccessMgmt.FFB.Jobs.Models;
using Dapper;
using Npgsql;

namespace Altinn.AccessMgmt.FFB.Jobs;

/// <summary>
/// Manually installs or rolls back the activity log schema in a test environment, using the SQL
/// generated from the ActivityLog and ActivityTypeCatalog EF migrations but without the
/// <c>__EFMigrationsHistory</c> bookkeeping — EF still considers the migrations unapplied.
/// Intended for trying the triggers, analysis and backfill in larger environments before the
/// real migrations ship. The dbo.activitytype catalog table is created but left unseeded
/// (seeding happens via StaticDataIngest when the release deploys); the runtime resolves the
/// catalog from ActivityTypeConstants in memory, so nothing depends on the table content.
/// Guards refuse both operations in any environment where EF has applied the migrations, and
/// the install refuses when dbo.activitylog already exists — so an EF-managed environment can
/// never be touched. Roll back before the real migrations run, since they are not idempotent.
/// The embedded scripts must be regenerated whenever an activity log migration changes.
/// </summary>
public static class ActivityLogSchemaJob
{
    public const string JobName = "ActivityLogSchema";

    private const string MigrationId = "20260901142849_ActivityLog";

    public static async Task InstallAsync(DuoRepo repo, JobRun run, CancellationToken ct)
    {
        await using var conn = repo.CreateAccConnection();
        await conn.OpenAsync(ct);

        if (await EfHasMigrationAsync(conn, ct))
        {
            run.AddLog($"The {MigrationId} migration is already applied through EF in this environment — manual install is only for pre-migration test environments.", isError: true);
            return;
        }

        if (await RelationExistsAsync(conn, "dbo.activitylog", ct))
        {
            run.AddLog("dbo.activitylog already exists — nothing to install.", isError: true);
            return;
        }

        run.AddLog("Installing the activity log schema (functions, tables, triggers, partitions) without EF migration bookkeeping...");
        await conn.ExecuteAsync(new CommandDefinition(ReadScript("activitylog-schema-install.sql"), commandTimeout: 0, cancellationToken: ct));
        run.AddLog("Schema installed — the triggers are live and logging from now. Roll back before the real EF migration is applied here.");
    }

    public static async Task RollbackAsync(DuoRepo repo, JobRun run, CancellationToken ct)
    {
        await using var conn = repo.CreateAccConnection();
        await conn.OpenAsync(ct);

        if (await EfHasMigrationAsync(conn, ct))
        {
            run.AddLog($"The {MigrationId} migration is applied through EF in this environment — refusing to roll back an EF-managed schema.", isError: true);
            return;
        }

        if (!await RelationExistsAsync(conn, "dbo.activitylog", ct))
        {
            run.AddLog("dbo.activitylog does not exist — nothing to roll back.", isError: true);
            return;
        }

        var rows = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT count(*) FROM dbo.activitylog;", commandTimeout: 0, cancellationToken: ct));

        run.AddLog($"Rolling back the activity log schema — dropping dbo.activitylog with {rows:N0} logged events, all triggers and support functions...");
        await conn.ExecuteAsync(new CommandDefinition(ReadScript("activitylog-schema-rollback.sql"), commandTimeout: 0, cancellationToken: ct));
        run.AddLog("Schema rolled back — the environment is back to its pre-activitylog state, and the EF migration can be applied normally later.");
    }

    private static async Task<bool> EfHasMigrationAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var hasHistoryTable = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT to_regclass('\"__EFMigrationsHistory\"') IS NOT NULL;", cancellationToken: ct));

        if (!hasHistoryTable)
        {
            return false;
        }

        return await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = @id);",
            new { id = MigrationId },
            cancellationToken: ct));
    }

    private static Task<bool> RelationExistsAsync(NpgsqlConnection conn, string relation, CancellationToken ct)
        => conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT to_regclass(@relation) IS NOT NULL;", new { relation }, cancellationToken: ct));

    private static string ReadScript(string name)
    {
        var assembly = typeof(ActivityLogSchemaJob).Assembly;
        var resource = $"Altinn.AccessMgmt.FFB.Jobs.SchemaScripts.{name}";
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded script '{resource}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
