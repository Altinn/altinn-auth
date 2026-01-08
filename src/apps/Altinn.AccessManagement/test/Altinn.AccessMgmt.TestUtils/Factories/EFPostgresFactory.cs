using Altinn.AccessManagement.Persistence.Configuration;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.Authorization.Host.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit.Sdk;

namespace Altinn.AccessMgmt.TestUtils.Factories;

/// <summary>
/// Test helper that provisions a PostgreSQL container and provides per-test
/// databases. On first use the factory starts a PostgreSQL container, creates
/// a migrated and seeded template database, and thereafter returns cloned
/// databases based on that template to provide isolation between tests.
/// </summary>
public static class EFPostgresFactory
{
    private static PostgreSqlContainer Server { get; } = new PostgreSqlBuilder()
        .WithCleanUp(true)
        .WithImage("docker.io/postgres:16.1-alpine")
        .Build();

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private static bool _isInitialized = false;

    private static int _databaseInstance = 0;

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

    /// <summary>
    /// Creates and returns a new database instance for a test. The first call
    /// initializes the container, sets up roles and a migrated template database
    /// named <c>test_primary</c>, and seeds it with test data. Subsequent calls
    /// create cloned databases using <c>CREATE DATABASE ... WITH TEMPLATE test_primary</c>.
    /// </summary>
    /// <returns>A <see cref="PostgresDatabase"/> describing the newly created database.</returns>
    /// <exception cref="XunitException">Thrown when bootstrapping the primary database or roles fails.</exception>
    public static async Task<PostgresDatabase> Create()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                await Server.StartAsync();

                const string templateDb = "test_primary";

                var roleResult = await Server.ExecScriptAsync($@"
                DO $$
                    BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{DbUserName}') THEN
                        CREATE ROLE {DbUserName} LOGIN PASSWORD '{DbPassword}' SUPERUSER INHERIT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{DbAdminName}') THEN
                        CREATE ROLE {DbAdminName} LOGIN PASSWORD '{DbPassword}' SUPERUSER INHERIT;
                    END IF;
                END $$;
                ");

                if (roleResult.ExitCode != 0 || !string.IsNullOrEmpty(roleResult.Stderr))
                {
                    throw new XunitException($"Role init failed. Exitcode {roleResult.ExitCode}, Error {roleResult.Stderr}");
                }

                var createDbResult = await Server.ExecScriptAsync($@"CREATE DATABASE {templateDb};");
                if (createDbResult.ExitCode != 0 || !string.IsNullOrEmpty(createDbResult.Stderr))
                {
                    throw new XunitException($"Create Db failed. Exitcode {createDbResult.ExitCode}, Error {createDbResult.Stderr}");
                }

                var connString = new PostgresDatabase(templateDb, Server.GetConnectionString());

                using var sp = new ServiceCollection()
                    .AddAccessManagementDatabase(opts =>
                    {
                        opts.MigrationConnectionString = connString.Admin.ToString();
                        opts.Source = SourceType.Migration;
                        opts.EnableEFPooling = false;
                    }).BuildServiceProvider();

                var audit = new AuditValues(SystemEntityConstants.StaticDataIngest);
                using var db = sp.CreateEFScope(audit).ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                await TestDataSeeds.Exec(db);
                NpgsqlConnection.ClearAllPools();
                _isInitialized = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        var dbInstance = Interlocked.Increment(ref _databaseInstance);
        var dbName = $"test_{dbInstance}";

        var cloneResult = await Server.ExecScriptAsync($"CREATE DATABASE {dbName} WITH TEMPLATE test_primary OWNER {DbUserName};");

        if (cloneResult.ExitCode != 0 || !string.IsNullOrEmpty(cloneResult.Stderr))
        {
            throw new XunitException($"create database with template failed. Exitcode {cloneResult.ExitCode}, Error {cloneResult.Stderr}");
        }

        return new PostgresDatabase(dbName, Server.GetConnectionString());
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
