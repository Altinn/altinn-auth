using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

namespace Altinn.Authorization.Tests.Fixtures;

/// <summary>
/// Provides a PostgreSQL database carrying the AuthorizationDB schema (the Yuniql
/// migration scripts shipped with <c>Altinn.Authorization</c>) so tests can
/// exercise <c>DelegationMetadataRepository</c> against a real database via
/// <see cref="ApplicationConnectionString"/>.
/// </summary>
/// <remarks>
/// <para>
/// Backed by the shared <see cref="PostgresTestEngine"/>: the migration scripts are
/// replayed once into a template database, and each test class gets a fast
/// <c>CREATE DATABASE ... WITH TEMPLATE</c> clone — instead of starting a container
/// and re-running every script per fixture.
/// </para>
/// <para>
/// On hosts where Docker / Testcontainers is unavailable, <see cref="SkipReason"/>
/// is populated and <see cref="ApplicationConnectionString"/> is left empty. Tests
/// must call <c>Assert.SkipWhen(fixture.SkipReason is not null, fixture.SkipReason!)</c>
/// at the top of their body — a skip thrown from <c>InitializeAsync</c> surfaces as
/// a fixture-init failure, not a skip, in xUnit v3, so the engine records the reason
/// instead of throwing and the check has to live on the per-test path.
/// </para>
/// </remarks>
public sealed class AuthorizationDbFixture : IAsyncLifetime
{
    private static readonly PostgresTestEngine Engine = new(new PostgresTestEngineOptions
    {
        // The application role stays a normal (non-superuser) login role — the
        // engine default — matching production and this fixture's prior behaviour,
        // so missing GRANTs surface instead of being masked.
        BuildTemplateAsync = ApplyMigrationsAsync,
    });

    /// <summary>
    /// Connection string for the application-level user, scoped to a clone of the
    /// migrated template. Empty when <see cref="SkipReason"/> is set.
    /// </summary>
    public string ApplicationConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Set when no container runtime is available. Tests should call
    /// <c>Assert.SkipWhen</c> on this before using <see cref="ApplicationConnectionString"/>.
    /// </summary>
    public string? SkipReason { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var database = await Engine.CreateDatabaseAsync();
        if (database is null)
        {
            SkipReason = Engine.SkipReason;
            return;
        }

        ApplicationConnectionString = database.User.ToString();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task ApplyMigrationsAsync(PostgresTestDatabase template)
    {
        await using var conn = new NpgsqlConnection(template.Admin.ToString());
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

    private static IEnumerable<string> EnumerateMigrationFiles()
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
