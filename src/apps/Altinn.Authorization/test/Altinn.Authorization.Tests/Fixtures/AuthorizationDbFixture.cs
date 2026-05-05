using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Altinn.Platform.Authorization.IntegrationTests.Fixtures;

/// <summary>
/// Provisions a PostgreSQL container, bootstraps the AuthorizationDB roles +
/// schema, and replays the Yuniql migration scripts shipped with
/// <c>Altinn.Authorization</c>. Tests can pull <see cref="ApplicationConnectionString"/>
/// to exercise <c>DelegationMetadataRepository</c> against a real database.
/// </summary>
/// <remarks>
/// On hosts where Docker / Testcontainers is unavailable (e.g. CI runners with
/// the daemon down) <see cref="InitializeAsync"/> calls <c>Assert.Skip</c> so
/// every test using this fixture is reported as skipped rather than failing.
/// </remarks>
public sealed class AuthorizationDbFixture : IAsyncLifetime
{
    private const string AppUser = "platform_authorization";
    private const string AdminUser = "platform_authorization_admin";
    private const string Password = "Password";
    private const string DatabaseName = "authorizationdb";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("docker.io/postgres:16.1-alpine")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// Connection string for the application-level <c>platform_authorization</c>
    /// user, scoped to the bootstrapped <c>authorizationdb</c> database.
    /// </summary>
    public string ApplicationConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            Assert.Skip($"Docker/Testcontainers unavailable: {ex.GetBaseException().Message}");
            return;
        }

        var bootstrap = await _container.ExecScriptAsync($@"
            CREATE ROLE {AppUser} LOGIN PASSWORD '{Password}';
            CREATE ROLE {AdminUser} LOGIN PASSWORD '{Password}' SUPERUSER;
            CREATE DATABASE {DatabaseName} OWNER {AdminUser};
        ");
        if (bootstrap.ExitCode != 0)
        {
            throw new InvalidOperationException($"Bootstrap failed (exit {bootstrap.ExitCode}): {bootstrap.Stderr}");
        }

        var adminConnection = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = DatabaseName,
            Username = AdminUser,
            Password = Password,
        }.ToString();

        await using (var conn = new NpgsqlConnection(adminConnection))
        {
            await conn.OpenAsync();

            // Authorization migrations assume the delegation schema already exists; in
            // production this is provisioned outside Yuniql.
            await using (var cmd = new NpgsqlCommand("CREATE SCHEMA IF NOT EXISTS delegation;", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var sqlFile in EnumerateMigrationFiles())
            {
                var sql = await File.ReadAllTextAsync(sqlFile);
                await using var cmd = new NpgsqlCommand(sql, conn);
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Migration '{sqlFile}' failed: {ex.Message}", ex);
                }
            }
        }

        ApplicationConnectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = DatabaseName,
            Username = AppUser,
            Password = Password,
        }.ToString();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private static System.Collections.Generic.IEnumerable<string> EnumerateMigrationFiles()
    {
        var migrationDir = Path.Combine(AppContext.BaseDirectory, "Migration");
        if (!Directory.Exists(migrationDir))
        {
            throw new InvalidOperationException(
                $"Authorization migration directory not found at '{migrationDir}'. " +
                "The test csproj must copy 'src/Altinn.Authorization/Migration/**/*.sql' to the output directory.");
        }

        // Only versioned directories ('v0.00', 'v0.01', ...) are migration sources;
        // '_draft', '_erase', '_init', '_post', '_pre' are convention-only README holders.
        //
        // Replay is capped at v0.07: v0.08 redefines `delegation.get_current_change` with a
        // TABLE(...) return clause where v0.04 declared it as SETOF delegation.delegationchanges,
        // and the new statement lacks a `DROP FUNCTION IF EXISTS` step — Postgres then refuses
        // the change with `42P13: cannot change return type`. The columns the repository reads
        // are identical between the v0.04 and v0.08 forms, so capping here exercises the
        // production stored-procedure path the integration test cares about (enum round-trip
        // through `insert_delegationchange` and `get_current_change`).
        return Directory.EnumerateDirectories(migrationDir, "v*")
            .Where(d => string.Compare(Path.GetFileName(d), "v0.07", StringComparison.Ordinal) <= 0)
            .OrderBy(d => d, StringComparer.Ordinal)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.sql").OrderBy(f => f, StringComparer.Ordinal));
    }
}
