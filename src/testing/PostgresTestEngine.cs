using Npgsql;
using Testcontainers.PostgreSql;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Shared PostgreSQL provisioning engine for integration tests. Starts one
/// Testcontainers PostgreSQL server, bootstraps the application + admin roles,
/// builds a migrated/seeded <em>template</em> database once, and then hands out
/// fast per-test clones via <c>CREATE DATABASE ... WITH TEMPLATE</c>.
/// </summary>
/// <remarks>
/// <para>
/// The engine is vertical-agnostic: callers supply how the template is built
/// (EF migrations + seed, Yuniql SQL, …) through
/// <see cref="PostgresTestEngineOptions.BuildTemplateAsync"/>. It is shared into
/// test assemblies as linked source (see <c>src/Directory.Build.targets</c>), so
/// each assembly gets its own static instance and container — no cross-vertical
/// project reference.
/// </para>
/// <para>
/// <strong>Docker-skip:</strong> when no container runtime is available the engine
/// does <em>not</em> throw — it records <see cref="SkipReason"/> and returns
/// <c>null</c> from <see cref="CreateDatabaseAsync"/>. Throwing (including
/// <c>Assert.Skip</c>) from a fixture's <c>InitializeAsync</c> surfaces as a
/// fixture-init failure, not a skip, in xUnit v3 — so the engine leaves the skip
/// decision to the caller, which should stash the reason and have each test call
/// <c>Assert.SkipWhen(fixture.SkipReason is not null, fixture.SkipReason!)</c>.
/// </para>
/// </remarks>
public sealed class PostgresTestEngine
{
    private readonly PostgresTestEngineOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private PostgreSqlContainer? _server;
    private bool _templateReady;
    private int _databaseInstance;

    /// <summary>
    /// Creates an engine with the supplied options. Construct one per test
    /// assembly (typically a <c>static readonly</c> field on a factory/fixture).
    /// </summary>
    public PostgresTestEngine(PostgresTestEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.BuildTemplateAsync);
        _options = options;
    }

    /// <summary>
    /// Set when the engine could not provision a container (typically: Docker /
    /// Podman not running). While set, <see cref="CreateDatabaseAsync"/> returns
    /// <c>null</c>. Callers should convert this to a per-test skip.
    /// </summary>
    public string? SkipReason { get; private set; }

    /// <summary>
    /// Returns a fresh database cloned from the shared template, or <c>null</c>
    /// when no container runtime is available (see <see cref="SkipReason"/>). The
    /// first call starts the container and builds the template under a lock;
    /// subsequent calls only clone.
    /// </summary>
    public async Task<PostgresTestDatabase?> CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (SkipReason is not null)
            {
                return null;
            }

            if (!_templateReady)
            {
                if (!await EnsureServerStartedAsync(cancellationToken))
                {
                    return null;
                }

                await FixtureTiming.TimeAsync(FixtureTiming.Phase.TemplateBuild, async () =>
                {
                    await BootstrapRolesAsync(cancellationToken);
                    await ExecAsync($"CREATE DATABASE {_options.TemplateDatabaseName};", cancellationToken);
                    var template = NewDatabase(_options.TemplateDatabaseName);
                    await _options.BuildTemplateAsync(template);
                    NpgsqlConnection.ClearAllPools();
                });
                _templateReady = true;
            }
        }
        finally
        {
            _gate.Release();
        }

        var name = $"test_{Interlocked.Increment(ref _databaseInstance)}";
        await FixtureTiming.TimeAsync(FixtureTiming.Phase.Clone, () => ExecAsync(
            $"CREATE DATABASE {name} WITH TEMPLATE {_options.TemplateDatabaseName} OWNER {_options.ApplicationUser};",
            cancellationToken));
        return NewDatabase(name);
    }

    private async Task<bool> EnsureServerStartedAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Both `.Build()` and `StartAsync` reach for the Docker daemon, so they
            // must be inside the try — a field initializer would surface as an
            // unrecoverable fixture-construction failure instead of a skip.
            _server = new PostgreSqlBuilder()
                .WithImage(_options.Image)
                .WithCleanUp(true)
                .Build();
            await _server.StartAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            SkipReason = $"Docker/Testcontainers unavailable: {ex.GetBaseException().Message}";
            return false;
        }
    }

    private async Task BootstrapRolesAsync(CancellationToken cancellationToken)
    {
        // Idempotent: a shared container builds roles once, but keep IF NOT EXISTS
        // so the script is safe to re-run. The application role is superuser only
        // when opted in; the admin role is always superuser.
        var applicationSuperuser = _options.ApplicationUserIsSuperuser ? "SUPERUSER " : string.Empty;
        await ExecAsync(
            $@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{_options.ApplicationUser}') THEN
                    CREATE ROLE {_options.ApplicationUser} LOGIN PASSWORD '{_options.Password}' {applicationSuperuser}INHERIT;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{_options.AdminUser}') THEN
                    CREATE ROLE {_options.AdminUser} LOGIN PASSWORD '{_options.Password}' SUPERUSER INHERIT;
                END IF;
            END $$;
            ",
            cancellationToken);
    }

    private async Task ExecAsync(string sql, CancellationToken cancellationToken)
    {
        var result = await _server!.ExecScriptAsync(sql, cancellationToken);
        if (result.ExitCode != 0 || !string.IsNullOrEmpty(result.Stderr))
        {
            throw new InvalidOperationException(
                $"PostgreSQL test setup failed (exit {result.ExitCode}): {result.Stderr}");
        }
    }

    private PostgresTestDatabase NewDatabase(string databaseName) =>
        new(databaseName, _server!.GetConnectionString(), _options);
}

/// <summary>
/// Configuration for a <see cref="PostgresTestEngine"/>. Roles, password, and
/// image default to the values both verticals already use; the one required
/// member is <see cref="BuildTemplateAsync"/>.
/// </summary>
public sealed class PostgresTestEngineOptions
{
    /// <summary>Container image. Defaults to <c>postgres:16.1-alpine</c>.</summary>
    public string Image { get; init; } = "docker.io/postgres:16.1-alpine";

    /// <summary>Application-level (non-admin) login role.</summary>
    public string ApplicationUser { get; init; } = "platform_authorization";

    /// <summary>Administrative login role used for migrations and template/clone creation.</summary>
    public string AdminUser { get; init; } = "platform_authorization_admin";

    /// <summary>Password for both roles.</summary>
    public string Password { get; init; } = "Password";

    /// <summary>
    /// Whether the application role is created as a superuser. Defaults to
    /// <c>false</c> — a normal login role, mirroring production, so missing GRANTs
    /// surface in integration tests instead of being masked. The admin role is
    /// always a superuser.
    /// </summary>
    public bool ApplicationUserIsSuperuser { get; init; }

    /// <summary>Name of the template database that clones are created from.</summary>
    public string TemplateDatabaseName { get; init; } = "test_primary";

    /// <summary>
    /// Applies schema + seed data to the template database. Runs exactly once,
    /// before any clone is created. Receives the template's connection builders.
    /// </summary>
    public required Func<PostgresTestDatabase, Task> BuildTemplateAsync { get; init; }
}

/// <summary>
/// A provisioned test database, with admin and application connection-string
/// builders. Returned by <see cref="PostgresTestEngine.CreateDatabaseAsync"/>.
/// </summary>
public sealed class PostgresTestDatabase
{
    internal PostgresTestDatabase(string name, string baseConnectionString, PostgresTestEngineOptions options)
    {
        Name = name;
        Admin = BuildConnection(baseConnectionString, options.AdminUser, options.Password, name);
        User = BuildConnection(baseConnectionString, options.ApplicationUser, options.Password, name);
    }

    /// <summary>Physical database name.</summary>
    public string Name { get; }

    /// <summary>Connection builder for the admin role (migrations, DDL).</summary>
    public NpgsqlConnectionStringBuilder Admin { get; }

    /// <summary>Connection builder for the application role.</summary>
    public NpgsqlConnectionStringBuilder User { get; }

    private static NpgsqlConnectionStringBuilder BuildConnection(string baseConnectionString, string user, string password, string database) =>
        new(baseConnectionString)
        {
            Database = database,
            Username = user,
            Password = password,
            IncludeErrorDetail = true,
            Timeout = 3,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 50,
            ConnectionIdleLifetime = 30,
            ConnectionPruningInterval = 15,
        };
}
