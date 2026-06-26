using System;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Altinn.Authorization.Tests.Fixtures;

/// <summary>
/// Provides a PostgreSQL database carrying the AuthorizationDB schema (the EF Core
/// migrations shipped with <c>Altinn.Authorization</c>) so tests can exercise
/// <c>DelegationMetadataRepository</c> against a real database via
/// <see cref="ApplicationConnectionString"/>.
/// </summary>
/// <remarks>
/// <para>
/// Backed by the shared <see cref="PostgresTestEngine"/>: the migrations are applied
/// once into a template database, and each test class gets a fast
/// <c>CREATE DATABASE ... WITH TEMPLATE</c> clone — instead of starting a container
/// and re-running migrations per fixture.
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
        var database = await FixtureTiming.TimeAsync(FixtureTiming.Phase.DbProvision, () => Engine.CreateDatabaseAsync());
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
        // Apply the EF Core migrations with the admin role — the baseline migration
        // creates the delegation schema, its enum type, table, functions and grants.
        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseNpgsql(template.Admin.ToString())
            .Options;

        await using var db = new AuthorizationDbContext(options);
        await db.Database.MigrateAsync();
    }
}
