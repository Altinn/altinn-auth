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
/// On hosts where Docker / Testcontainers is unavailable, <see cref="SkipReason"/>
/// is populated and <see cref="ApplicationConnectionString"/> is left empty.
/// Tests should call <c>Assert.SkipWhen(fixture.SkipReason is not null, fixture.SkipReason!)</c>
/// at the top of their body — <c>Assert.Skip</c> raised from
/// <see cref="IAsyncLifetime.InitializeAsync"/> propagates as a fixture-init
/// failure rather than a test skip in xUnit v3, so the check has to live on the
/// per-test path.
/// </remarks>
public sealed class AuthorizationDbFixture : IAsyncLifetime
{
    private const string AppUser = "platform_authorization";
    private const string AdminUser = "platform_authorization_admin";
    private const string Password = "Password";
    private const string DatabaseName = "authorizationdb";

    private PostgreSqlContainer? _container;

    /// <summary>
    /// Connection string for the application-level <c>platform_authorization</c>
    /// user, scoped to the bootstrapped <c>authorizationdb</c> database.
    /// Empty when <see cref="SkipReason"/> is set.
    /// </summary>
    public string ApplicationConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Set when the fixture cannot provision a real PostgreSQL container
    /// (typically: Docker daemon not running). Tests that depend on the
    /// fixture should call <c>Assert.SkipWhen</c> on this value before doing
    /// any work that requires <see cref="ApplicationConnectionString"/>.
    /// </summary>
    public string? SkipReason { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        try
        {
            // Both the builder's `.Build()` and `StartAsync` reach for the Docker
            // daemon, so the construction has to live inside the try/catch — running
            // it as a field initializer would surface a fixture-construction
            // failure that no test-level skip can intercept.
            _container = new PostgreSqlBuilder()
                .WithImage("docker.io/postgres:16.1-alpine")
                .WithCleanUp(true)
                .Build();
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            // Throwing — including via Assert.Skip — from IAsyncLifetime.InitializeAsync
            // surfaces as a fixture-init failure (every dependent test is reported
            // failed with "Class fixture type ... threw"), not as a skip. Stash the
            // reason and let each test convert it to a skip via Assert.SkipWhen.
            SkipReason = $"Docker/Testcontainers unavailable: {ex.GetBaseException().Message}";
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
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
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
        return Directory.EnumerateDirectories(migrationDir, "v*")
            .OrderBy(d => d, StringComparer.Ordinal)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.sql").OrderBy(f => f, StringComparer.Ordinal));
    }
}
