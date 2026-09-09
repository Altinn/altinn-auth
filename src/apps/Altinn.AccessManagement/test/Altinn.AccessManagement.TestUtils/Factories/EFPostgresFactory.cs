using Altinn.AccessManagement.Persistence.Configuration;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.Authorization.Host.Database;
using Altinn.Authorization.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Altinn.AccessManagement.TestUtils.Factories;

/// <summary>
/// Test helper that provisions per-test databases backed by the shared
/// <see cref="PostgresTestEngine"/>: on first use it starts a PostgreSQL
/// container, builds a migrated and seeded template database, and thereafter
/// returns fast clones of that template.
/// </summary>
public static class EFPostgresFactory
{
    /// <summary>
    /// Password used for the test database users.
    /// </summary>
    public static readonly string DbPassword = "Password";

    /// <summary>
    /// Application-level database user (non-admin) used by tests.
    /// </summary>
    public static readonly string DbUserName = "platform_authorization";

    /// <summary>
    /// Administrative database user used when performing migrations and other privileged operations.
    /// </summary>
    public static readonly string DbAdminName = "platform_authorization_admin";

    private static readonly PostgresTestEngine _engine = new(new PostgresTestEngineOptions
    {
        ApplicationUser = DbUserName,
        AdminUser = DbAdminName,
        Password = DbPassword,

        // AccessManagement's test app role has always been a superuser; keep it.
        ApplicationUserIsSuperuser = true,
        BuildTemplateAsync = BuildTemplateAsync,
    });

    /// <summary>
    /// Creates and returns a new database instance for a test, cloned from the
    /// shared migrated + seeded template. The first call starts the container and
    /// builds the template; subsequent calls only clone.
    /// </summary>
    /// <returns>A <see cref="PostgresDatabase"/> describing the newly created database.</returns>
    public static async Task<PostgresDatabase> Create()
    {
        var database = await _engine.CreateDatabaseAsync();
        if (database is null)
        {
            // No container runtime. The engine records the reason rather than
            // throwing; surface it as a skip here (note: a skip thrown from a
            // fixture's InitializeAsync still reports as a fixture-init failure in
            // xUnit v3 — converting ApiFixture to a per-test skip is tracked
            // separately).
            Assert.Skip(_engine.SkipReason ?? "Docker/Testcontainers unavailable");
            return null!;
        }

        return new PostgresDatabase(database.Name, database.Admin.ToString());
    }

    private static async Task BuildTemplateAsync(PostgresTestDatabase template)
    {
        using var sp = new ServiceCollection()
            .AddAccessManagementDatabase(opts =>
            {
                opts.MigrationConnectionString = template.Admin.ToString();
                opts.Source = SourceType.Migration;
                opts.EnableEFPooling = false;
            }).BuildServiceProvider();

        var audit = new AuditValues(SystemEntityConstants.StaticDataIngest);
        using var db = sp.CreateEFScope(audit).ServiceProvider.GetRequiredService<AppDbContext>();
        await FixtureTiming.TimeAsync(FixtureTiming.Phase.Migrate, () => db.Database.MigrateAsync());
        await FixtureTiming.TimeAsync(FixtureTiming.Phase.Seed, () => TestDataSeeds.Exec(db));
    }
}

/// <summary>
/// Container that provides connection string builders for both the admin and
/// application user for a specific test database. Also implements
/// <see cref="IOptions{PostgreSQLSettings}"/> so it can be injected where
/// configuration of the test database is required.
/// </summary>
/// <param name="dbname">The name of the physical database.</param>
/// <param name="connectionString">Base connection string returned from the test container.</param>
public class PostgresDatabase(string dbname, string connectionString) : IOptions<PostgreSQLSettings>
{
    /// <summary>
    /// Connection string builder configured for administrative actions (migrations, template creation).
    /// </summary>
    public NpgsqlConnectionStringBuilder Admin { get; } = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Timeout = 3,
        Database = dbname,
        Username = EFPostgresFactory.DbAdminName,
        Password = EFPostgresFactory.DbPassword,
        IncludeErrorDetail = true,
        Pooling = true,
        ConnectionIdleLifetime = 30,
        MinPoolSize = 0,
        MaxPoolSize = 50,
        ConnectionPruningInterval = 15,
    };

    /// <summary>
    /// Connection string builder configured for the application-level test user.
    /// </summary>
    public NpgsqlConnectionStringBuilder User { get; } = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Timeout = 3,
        Database = dbname,
        Username = EFPostgresFactory.DbUserName,
        Password = EFPostgresFactory.DbPassword,
        IncludeErrorDetail = true,
        Pooling = true,
        ConnectionIdleLifetime = 30,
        MinPoolSize = 0,
        MaxPoolSize = 50,
        ConnectionPruningInterval = 15,
    };

    /// <summary>
    /// Returns a <see cref="PostgreSQLSettings"/> instance populated with the
    /// user connection string and password, suitable for injection into
    /// components that consume <see cref="IOptions{PostgreSQLSettings}"/>.
    /// </summary>
    public PostgreSQLSettings Value => new()
    {
        ConnectionString = User.ToString(),
        AuthorizationDbPwd = User.Password
    };
}
