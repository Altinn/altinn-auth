using Altinn.AccessMgmt.PersistenceEF.Models;
using Dapper;
using Npgsql;

namespace Altinn.AccessMgmt.FFB.Jobs;

// ── Dapper result DTOs (records — immutable value objects) ────────────────────
public record IngestTable(string Name);

public record JobEntity(Guid Id);

public record JobAssignment(Guid FromId, Guid ToId, string RoleCode);

// ValidFrom is DateTime (not DateTimeOffset): Npgsql returns timestamptz as a UTC DateTime, and
// Dapper matches this positional record's constructor by column type. Implicitly widens to
// DateTimeOffset when passed to UpsertAssignment.
public record JobAssignmentInstance(Guid Id, Guid AssignmentId, Guid AssignmentFromId, Guid AssignmentToId, DateTime ValidFrom, Guid ChangedBy, Guid ChangedBySystem);

public record JobRole(Guid Id, string Name, string Code);

public record KeyVal(string Key, long Val);

// ── Struct keys and comparers ─────────────────────────────────────────────────
public readonly record struct AssignmentKey(Guid FromId, Guid ToId, string RoleCode);

public readonly record struct AssignmentGroupCountKey(string RoleCode, long AccCount, long RegCount);

public sealed class AssignmentKeyComparer : IEqualityComparer<AssignmentKey>
{
    public static readonly AssignmentKeyComparer Ordinal = new();

    public bool Equals(AssignmentKey x, AssignmentKey y) =>
        x.FromId == y.FromId &&
        x.ToId == y.ToId &&
        string.Equals(x.RoleCode, y.RoleCode, StringComparison.Ordinal);

    public int GetHashCode(AssignmentKey obj)
    {
        unchecked
        {
            var hash = obj.FromId.GetHashCode();
            hash = (hash * 397) ^ obj.ToId.GetHashCode();
            hash = (hash * 397) ^ (obj.RoleCode is not null
                ? StringComparer.Ordinal.GetHashCode(obj.RoleCode) : 0);
            return hash;
        }
    }
}

public sealed class AssignmentGroupCountKeyComparer : IEqualityComparer<AssignmentGroupCountKey>
{
    public static readonly AssignmentGroupCountKeyComparer Ordinal = new();

    public bool Equals(AssignmentGroupCountKey x, AssignmentGroupCountKey y) =>
        string.Equals(x.RoleCode, y.RoleCode, StringComparison.Ordinal) &&
        x.AccCount == y.AccCount &&
        x.RegCount == y.RegCount;

    public int GetHashCode(AssignmentGroupCountKey obj)
    {
        unchecked
        {
            var hash = obj.RoleCode is not null
                ? StringComparer.Ordinal.GetHashCode(obj.RoleCode) : 0;
            hash = (hash * 397) ^ obj.AccCount.GetHashCode();
            hash = (hash * 397) ^ obj.RegCount.GetHashCode();
            return hash;
        }
    }
}

// ── DuoRepo ───────────────────────────────────────────────────────────────────
public sealed class DuoRepo(string accConnString, string? regConnString = null)
{
    // ── Known filter GUIDs (AccessMgmt) ──────────────────────────────────────
    /// <summary>Provider IDs whose roles are included in assignment sync (Altinn 3 role providers).</summary>
    private const string SyncProviderA = "0195ea92-2080-758b-89db-7735c4f68320";
    private const string SyncProviderB = "019bbcab-449f-749a-bc13-4218549b8e93";

    /// <summary>Entity type IDs included in entity sync (Person, Organization).</summary>
    private const string EntityTypeOrg = "8c216e2f-afdd-4234-9ba2-691c727bb33d";
    private const string EntityTypePerson = "bfe09e70-e868-44b3-8d81-dfe0e13e058a";

    // ── Roles ─────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<JobRole>> GetAccRoles(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        return await conn.QueryAsync<JobRole>(new CommandDefinition(
            "SELECT id, name, code FROM dbo.role;",
            commandTimeout: 0, cancellationToken: ct));
    }

    // ── Assignments ───────────────────────────────────────────────────────────
    public async Task<IEnumerable<JobAssignment>> GetAccAssignments(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        const string sql = $"""
            SELECT a.fromid, a.toid, r.code AS rolecode
            FROM dbo.assignment AS a
            INNER JOIN dbo.role AS r ON a.roleid = r.id
            WHERE r.providerid IN ('{SyncProviderA}', '{SyncProviderB}');
            """;
        return await conn.QueryAsync<JobAssignment>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    public async Task<IEnumerable<KeyVal>> CountGroupedAccAssignments(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        const string sql = $"""
            SELECT r.code AS key, count(*) AS val
            FROM dbo.assignment AS a
            INNER JOIN dbo.role AS r ON a.roleid = r.id
            WHERE r.providerid IN ('{SyncProviderA}', '{SyncProviderB}')
            GROUP BY r.code;
            """;
        return await conn.QueryAsync<KeyVal>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    public async Task<Guid?> UpsertAssignment(Guid fromId, Guid toId, Guid roleId, DateTimeOffset validFrom, Guid changedBy, Guid changedBySystem, string changeOperation, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string auditSetupSql = """
        CREATE TEMP TABLE IF NOT EXISTS session_audit_context (
        changed_by UUID,
        changed_by_system UUID,
        change_operation_id text
        ) ON COMMIT DROP;

        TRUNCATE session_audit_context;

        INSERT INTO session_audit_context(changed_by, changed_by_system, change_operation_id)
        VALUES(@changedBy, @changedBySystem, @changeOperation);

        SELECT set_config('app.changed_by', @changedBy::text, false),
               set_config('app.changed_by_system', @changedBySystem::text, false),
               set_config('app.change_operation_id', @changeOperation, false);
        """;

        const string insertSql = """
        INSERT INTO dbo.assignment (id, fromid, toid, roleid, audit_validfrom, audit_changedby, audit_changedbysystem, audit_changeoperation)
        VALUES (@id, @fromId, @toId, @roleId, @validFrom, @changedBy, @changedBySystem, @changeOperation)
        ON CONFLICT (fromid, toid, roleid) DO NOTHING
        RETURNING id;
        """;

        const string selectSql = """
        SELECT id FROM dbo.assignment
        WHERE fromid = @fromId AND toid = @toId AND roleid = @roleId;
        """;

        var parameters = new
        {
            id = Guid.CreateVersion7(),
            fromId,
            toId,
            roleId,
            validFrom,
            changedBy,
            changedBySystem,
            changeOperation
        };

        await conn.ExecuteAsync(new CommandDefinition(auditSetupSql, parameters, transaction: tx, cancellationToken: ct));

        var id = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(insertSql, parameters, transaction: tx, cancellationToken: ct));
        if (id is null || id == Guid.Empty)
        {
            id = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(selectSql, parameters, transaction: tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<bool> RemoveAssignmentIfEmpty(Guid assignmentId, Guid changedBy, Guid changedBySystem, string changeOperation)
    {
        await using var conn = new NpgsqlConnection(accConnString);

        const string existSql = $"""
        select 'instance' from dbo.assignmentinstance where assignmentid = @assignmentId
        union all
        select 'resource' from dbo.assignmentresource where assignmentid = @assignmentId
        union all
        select 'package' from dbo.assignmentpackage where assignmentid = @assignmentId
        union all
        select 'delegation' from dbo.delegation where fromid = @assignmentId or toid = @assignmentId
        """;

        var exists = await conn.QueryAsync<string>(new CommandDefinition(existSql, new { assignmentId }));
        if (exists.Any())
        {
            return false;
        }

        const string deleteSql = """
        BEGIN TRANSACTION;

        CREATE TEMP TABLE IF NOT EXISTS session_audit_context (
        changed_by UUID,
        changed_by_system UUID,
        change_operation_id text
        ) ON COMMIT DROP;

        TRUNCATE session_audit_context;

        INSERT INTO session_audit_context(changed_by, changed_by_system, change_operation_id)
        VALUES(@changedBy, @changedBySystem, @changeOperation);

        SELECT set_config('app.changed_by', @changedBy::text, false),
               set_config('app.changed_by_system', @changedBySystem::text, false),
               set_config('app.change_operation_id', @changeOperation, false);

        WITH deleted AS (
            DELETE FROM dbo.assignment WHERE id = @assignmentId RETURNING id
        )
        SELECT count(*)::int FROM deleted;

        COMMIT TRANSACTION;
        """;

        var parameters = new { assignmentId, changedBy, changedBySystem, changeOperation };

        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(deleteSql, parameters));
        await multi.ReadAsync();                          // consume the set_config result set
        var affected = (await multi.ReadAsync<int>()).Single();
        return affected > 0;
    }

    public async Task<bool> UpdateAssignmentInstance(Guid assignmentInstanceId, Guid newAssignmentId, DateTimeOffset validFrom, Guid changedBy, Guid changedBySystem, string changeOperation)
    {
        await using var conn = new NpgsqlConnection(accConnString);

        const string updateSql = """
        BEGIN TRANSACTION;

        CREATE TEMP TABLE IF NOT EXISTS session_audit_context (
        changed_by UUID,
        changed_by_system UUID,
        change_operation_id text
        ) ON COMMIT DROP;

        TRUNCATE session_audit_context;

        INSERT INTO session_audit_context(changed_by, changed_by_system, change_operation_id)
        VALUES(@changedBy, @changedBySystem, @changeOperation);

        SELECT set_config('app.changed_by', @changedBy::text, false),
               set_config('app.changed_by_system', @changedBySystem::text, false),
               set_config('app.change_operation_id', @changeOperation, false);

        WITH updated AS (
            UPDATE dbo.assignmentinstance SET assignmentid = @newAssignmentId WHERE id = @assignmentInstanceId RETURNING id
        )
        SELECT count(*)::int FROM updated;

        COMMIT TRANSACTION;
        """;

        var parameters = new { assignmentInstanceId, newAssignmentId, changedBy, changedBySystem, changeOperation };

        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(updateSql, parameters));
        await multi.ReadAsync();                          // consume the set_config result set
        var affected = (await multi.ReadAsync<int>()).Single();
        return affected > 0;
    }

    public async Task<bool> DeleteAssignmentInstance(Guid assignmentInstanceId, Guid changedBy, Guid changedBySystem, string changeOperation)
    {
        await using var conn = new NpgsqlConnection(accConnString);

        // Same audit-context preamble as the other mutations (the DELETE fires the history trigger,
        // which reads both the GUCs and session_audit_context). Affected-row count read via RETURNING CTE.
        const string deleteSql = """
        BEGIN TRANSACTION;

        CREATE TEMP TABLE IF NOT EXISTS session_audit_context (
        changed_by UUID,
        changed_by_system UUID,
        change_operation_id text
        ) ON COMMIT DROP;

        TRUNCATE session_audit_context;

        INSERT INTO session_audit_context(changed_by, changed_by_system, change_operation_id)
        VALUES(@changedBy, @changedBySystem, @changeOperation);

        SELECT set_config('app.changed_by', @changedBy::text, false),
               set_config('app.changed_by_system', @changedBySystem::text, false),
               set_config('app.change_operation_id', @changeOperation, false);

        WITH deleted AS (
            DELETE FROM dbo.assignmentinstance WHERE id = @assignmentInstanceId RETURNING id
        )
        SELECT count(*)::int FROM deleted;

        COMMIT TRANSACTION;
        """;

        var parameters = new { assignmentInstanceId, changedBy, changedBySystem, changeOperation };

        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(deleteSql, parameters));
        await multi.ReadAsync();                          // consume the set_config result set
        var affected = (await multi.ReadAsync<int>()).Single();
        return affected > 0;
    }

    public async Task<IEnumerable<JobAssignmentInstance>> GetAccAssignmentInstance(Guid assignmentRoleId, Guid changedBySystemId, int limit = 1, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        const string sql = """
            SELECT
                 ai.id AS Id
                ,ai.assignmentid AS AssignmentId
                ,a.fromid AS AssignmentFromId
                ,a.toid AS AssignmentToId
                ,ai.audit_validfrom AS ValidFrom
                ,ai.audit_changedby AS ChangedBy
                ,ai.audit_changedbysystem AS ChangedBySystem
            FROM dbo.assignmentinstance ai
            INNER JOIN dbo.assignment a ON a.id = ai.assignmentid
            WHERE ai.audit_changedbysystem = @changedBySystemId
              AND a.roleid = @assignmentRoleId
            ORDER BY ai.id
            LIMIT @limit
            """;

        var parameters = new { assignmentRoleId, changedBySystemId, limit };

        return await conn.QueryAsync<JobAssignmentInstance>(new CommandDefinition(sql, parameters, commandTimeout: 0, cancellationToken: ct));
    }

    // ── Entities ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<JobEntity>> GetAccEntity(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        const string sql = $"""
            SELECT id
            FROM dbo.entity
            WHERE typeid IN ('{EntityTypeOrg}', '{EntityTypePerson}');
            """;
        return await conn.QueryAsync<JobEntity>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    // ── Ingest tables ─────────────────────────────────────────────────────────
    public async Task<IEnumerable<IngestTable>> GetIngestTables(
        string? filter,
        int size,
        CancellationToken ct = default)
    {
        size = size <= 0 ? 10_000 : size;
        var filterPattern = filter?.ToLowerInvariant() switch
        {
            "assignment" => "assignment_%",
            "entity" => "entity_%",
            "provider" => "provider_%",
            _ => "%"
        };

        await using var conn = new NpgsqlConnection(accConnString);
        var sql = $"""
            SELECT tablename AS name
            FROM pg_tables
            WHERE schemaname = 'ingest'
              AND tablename ILIKE '{filterPattern}'
            ORDER BY tablename DESC
            LIMIT {size};
            """;
        return await conn.QueryAsync<IngestTable>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    public async Task<int> DropIngestTable(string tableName, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        return await conn.ExecuteAsync(new CommandDefinition(
            $"DROP TABLE IF EXISTS ingest.{tableName} CASCADE;",
            commandTimeout: 0, cancellationToken: ct));
    }

    // ── History tables ────────────────────────────────────────────────────────
    /// <summary>Returns all distinct entity IDs that have at least one expired history row.</summary>
    public async Task<IReadOnlyList<Guid>> GetHistoryEntityIds(
        string tableName,
        CancellationToken ct = default)
    {
        var sql = $"SELECT DISTINCT id FROM {tableName} WHERE audit_validto IS NOT NULL ORDER BY id";
        await using var conn = new NpgsqlConnection(accConnString);
        return (await conn.QueryAsync<Guid>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct))).ToList();
    }

    /// <summary>
    /// Returns expired history rows for a single entity, ordered by audit_validfrom.
    /// Capped at <paramref name="limit"/> rows to avoid loading runaway history for one entity.
    /// </summary>
    public async Task<IReadOnlyList<IDictionary<string, object>>> GetHistoryRowsForEntity(
        string tableName,
        IEnumerable<string> dataColumns,
        Guid entityId,
        int limit = 10_000,
        CancellationToken ct = default)
    {
        var colList = string.Join(", ", dataColumns);
        var sql = $"""
            SELECT id, audit_validfrom, audit_validto, {colList}
            FROM {tableName}
            WHERE audit_validto IS NOT NULL
              AND id = @entityId
            ORDER BY audit_validfrom
            LIMIT @limit
            """;
        await using var conn = new NpgsqlConnection(accConnString);
        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { entityId, limit }, commandTimeout: 0, cancellationToken: ct));
        return rows.Cast<IDictionary<string, object>>().ToList();
    }

    // ── Generic query / execute ───────────────────────────────────────────────
    /// <summary>Executes a single SQL statement against AccessMgmt.</summary>
    public async Task<int> ExecuteSql(string sql, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        return await conn.ExecuteAsync(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    /// <summary>Executes a single SQL statement against Register.</summary>
    public async Task<int> ExecuteRegSql(string sql, CancellationToken ct = default)
    {
        await using var conn = OpenReg();
        return await conn.ExecuteAsync(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    /// <summary>Runs an arbitrary SELECT query against AccessMgmt and returns typed results.</summary>
    public async Task<IReadOnlyList<T>> QueryAccAsync<T>(
        string sql,
        object? param = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(accConnString);
        return (await conn.QueryAsync<T>(new CommandDefinition(sql, param, commandTimeout: 0, cancellationToken: ct))).ToList();
    }

    // ── Register ──────────────────────────────────────────────────────────────
    public async Task<IEnumerable<JobAssignment>> GetRegAssignments(CancellationToken ct = default)
    {
        await using var conn = OpenReg();
        const string sql = """
            SELECT from_party AS fromid, to_party AS toid, identifier AS rolecode
            FROM register.external_role_assignment;
            """;
        return await conn.QueryAsync<JobAssignment>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    public async Task<IEnumerable<KeyVal>> CountGroupedRegAssignments(CancellationToken ct = default)
    {
        await using var conn = OpenReg();
        const string sql = """
            SELECT identifier AS key, count(*) AS val
            FROM register.external_role_assignment
            GROUP BY identifier;
            """;
        return await conn.QueryAsync<KeyVal>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    public async Task<IEnumerable<JobEntity>> GetRegEntity(CancellationToken ct = default)
    {
        await using var conn = OpenReg();
        const string sql = """
            SELECT uuid AS id
            FROM register.party
            WHERE party_type IN ('person', 'organization');
            """;
        return await conn.QueryAsync<JobEntity>(new CommandDefinition(sql, commandTimeout: 0, cancellationToken: ct));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private NpgsqlConnection OpenReg() =>
        string.IsNullOrWhiteSpace(regConnString)
            ? throw new InvalidOperationException("Register connection string is not configured for this environment.")
            : new NpgsqlConnection(regConnString);
}
