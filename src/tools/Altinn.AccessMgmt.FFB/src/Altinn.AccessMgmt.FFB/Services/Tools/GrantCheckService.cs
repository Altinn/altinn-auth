using Altinn.AccessMgmt.FFB.Services.Contracts;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.Tools;

/// <summary>
/// Checks that database roles hold the expected privileges on every table in the
/// EF model, and executes the GRANT statements that close the gaps.
/// Roles and privileges come from <c>TableGrantConstants</c> (shared with the
/// migration SQL generator) and are passed in by the page.
/// </summary>
public sealed class GrantCheckService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Checks one environment. Throws when a requested role does not exist in the
    /// database (the page reports it as a per-environment error).
    /// </summary>
    public async Task<GrantCheckEnvironmentResult> CheckEnvironmentAsync(
        string environment,
        IReadOnlyList<string> schemas,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> privileges,
        CancellationToken ct = default)
    {
        var roleList = roles.ToList();
        var privilegeList = privileges.ToList();

        using var db = dbFactory.CreateContext(environment);
        var conn = db.Database.GetDbConnection();

        // has_table_privilege throws if a role does not exist — verify up front.
        // Dapper only honors the token through a CommandDefinition.
        var existingRoles = (await conn.QueryAsync<string>(new CommandDefinition(
            "SELECT rolname FROM pg_roles WHERE rolname = ANY(@roles)", new { roles = roleList }, cancellationToken: ct)))
            .ToHashSet(StringComparer.Ordinal);

        var missingRoles = roleList.Where(r => !existingRoles.Contains(r)).ToList();
        if (missingRoles.Count > 0)
        {
            throw new InvalidOperationException($"Rollene finnes ikke i databasen: {string.Join(", ", missingRoles)}.");
        }

        var missing = new List<TableGrant>();
        var missingFromDb = new List<MissingTable>();
        var passedCount = 0;

        foreach (var schema in schemas)
        {
            var modelTables = db.Model.GetEntityTypes()
                .Where(t => string.Equals(t.GetSchema(), schema, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.GetTableName())
                .Where(t => !string.IsNullOrEmpty(t))
                .Select(t => t!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();

            if (modelTables.Count == 0)
            {
                continue;
            }

            // Only check tables that actually exist in the DB — model may be ahead of the environment.
            var dbTables = (await conn.QueryAsync<string>(new CommandDefinition(
                "SELECT tablename FROM pg_tables WHERE schemaname = @schema", new { schema }, cancellationToken: ct)))
                .ToHashSet(StringComparer.Ordinal);

            var toCheck = modelTables.Where(dbTables.Contains).ToList();
            missingFromDb.AddRange(modelTables
                .Where(t => !dbTables.Contains(t))
                .Select(t => new MissingTable(environment, schema, t)));

            if (toCheck.Count == 0)
            {
                continue;
            }

            // Npgsql binds arrays natively, so pass tables/roles/privileges as parameters.
            const string sql = """
                SELECT t.tbl AS "Table", r.role AS "Role", p.priv AS "Privilege"
                FROM unnest(@tables::text[]) AS t(tbl)
                CROSS JOIN unnest(@roles::text[]) AS r(role)
                CROSS JOIN unnest(@privs::text[]) AS p(priv)
                WHERE NOT has_table_privilege(r.role, format('%I.%I', @schema, t.tbl), p.priv)
                ORDER BY t.tbl, r.role, p.priv
                """;

            var rows = (await conn.QueryAsync<GrantRow>(new CommandDefinition(
                sql,
                new { tables = toCheck, roles = roleList, privs = privilegeList, schema },
                cancellationToken: ct))).ToList();
            var byTable = rows.GroupBy(r => r.Table, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(r => (r.Role, r.Privilege)).ToList(), StringComparer.Ordinal);

            missing.AddRange(toCheck
                .Where(byTable.ContainsKey)
                .Select(t => new TableGrant { Environment = environment, Schema = schema, Table = t, Missing = byTable[t] }));
            passedCount += toCheck.Count - byTable.Count;
        }

        return new GrantCheckEnvironmentResult(passedCount, missing, missingFromDb);
    }

    /// <summary>
    /// Executes the GRANT statements for one table against the table's environment.
    /// </summary>
    public async Task FixTableAsync(TableGrant table, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(table.Environment);
        var conn = db.Database.GetDbConnection();
        foreach (var statement in GrantStatements(table))
        {
            await conn.ExecuteAsync(new CommandDefinition(statement, cancellationToken: ct));
        }
    }

    /// <summary>
    /// Distinct schemas in the (environment-independent) EF model, used for the
    /// schema checkboxes. Falls back to the known schemas when no environment is configured.
    /// </summary>
    public IReadOnlyList<string> GetModelSchemas()
    {
        var modelEnv = dbFactory.Environments.FirstOrDefault(dbFactory.IsConfigured);
        try
        {
            using var db = dbFactory.CreateContext(modelEnv!);
            return db.Model.GetEntityTypes()
                .Select(t => t.GetSchema())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
        }
        catch
        {
            return ["dbo", "dbo_history"];
        }
    }

    /// <summary>
    /// One GRANT per role, listing only the privileges that are missing for that role.
    /// Public so the page can render the SQL preview.
    /// </summary>
    public static IEnumerable<string> GrantStatements(TableGrant table) =>
        table.Missing
            .GroupBy(m => m.Role, StringComparer.Ordinal)
            .Select(g => GrantSql(
                table.Schema,
                table.Table,
                g.Key,
                g.Select(m => m.Privilege).Distinct(StringComparer.Ordinal)));

    private static string GrantSql(string schema, string table, string role, IEnumerable<string> privileges) =>
        $"GRANT {string.Join(", ", privileges)} ON TABLE {Quote(schema)}.{Quote(table)} TO {Quote(role)};";

    private static string Quote(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"")}\"";

    private sealed class GrantRow
    {
        public string Table { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Privilege { get; set; } = string.Empty;
    }
}
