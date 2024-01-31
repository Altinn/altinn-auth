using Altinn.ResourceRegistry.Persistence.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Yuniql.Core;

namespace Altinn.ResourceRegistry.TestUtils;

public class DbFixture 
    : IAsyncLifetime
{
    Singleton.Ref<Inner>? _inner;

    public async Task InitializeAsync()
    {
        _inner = await Singleton.Get<Inner>();
    }

    public Task<OwnedDb> CreateDbAsync()
        => _inner!.Value.CreateDbAsync();

    public async Task<IAsyncDisposable> ConfigureServicesAsync(IServiceCollection services)
    {
        var db = await CreateDbAsync();
        db.ConfigureServices(services);

        return db;
    }

    public async Task DisposeAsync()
    {
        if (_inner is { } inner)
        {
            await inner.DisposeAsync();
        }
    }

    private class Inner : IAsyncLifetime
    {
        private int _dbCounter = 0;
        private readonly AsyncLock _dbLock = new();
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("docker.io/postgres:16.1-alpine")
            .WithCleanUp(true)
            .Build();

        string? _connectionString;
        NpgsqlDataSource? _db;

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
            _connectionString = _dbContainer.GetConnectionString();
            _db = NpgsqlDataSource.Create(_connectionString);
        }

        public async Task<OwnedDb> CreateDbAsync()
        {
            var dbName = $"test_{Interlocked.Increment(ref _dbCounter)}";

            // only create 1 db at once
            using var guard = await _dbLock.Acquire();

            await using var cmd = _db!.CreateCommand(/*strpsql*/$"CREATE DATABASE {dbName};");

            await cmd.ExecuteNonQueryAsync();

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_connectionString) { Database = dbName, IncludeErrorDetail = true };
            var connectionString = connectionStringBuilder.ToString();

            var configuration = new Yuniql.AspNetCore.Configuration();
            configuration.Platform = SUPPORTED_DATABASES.POSTGRESQL;
            configuration.Workspace = Path.Combine(FindWorkspace(), "src", "Altinn.ResourceRegistry.Persistence", "Migration");
            configuration.ConnectionString = connectionString;
            configuration.IsAutoCreateDatabase = false;
            configuration.Environment = "integrationtest";

            var traceService = TraceService.Instance;
            var dataService = new Yuniql.PostgreSql.PostgreSqlDataService(traceService);
            var bulkImportService = new Yuniql.PostgreSql.PostgreSqlBulkImportService(traceService);
            var migrationServiceFactory = new MigrationServiceFactory(traceService);
            var migrationService = migrationServiceFactory.Create(dataService, bulkImportService);
            ConfigurationHelper.Initialize(configuration);
            migrationService.Run();

            return new OwnedDb(connectionString, dbName, _db);
        }

        public async Task DisposeAsync()
        {
            if (_db is { } db)
            {
                await db.DisposeAsync();
            }

            await _dbContainer.DisposeAsync();

            _dbLock.Dispose();
        }

        static string FindWorkspace()
        {
            var dir = Environment.CurrentDirectory;
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir, ".git")))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new InvalidOperationException("Workspace directory not found");
        }
    }

    public sealed class OwnedDb : IAsyncDisposable
    {
        readonly string _connectionString;
        readonly string _dbName;
        readonly NpgsqlDataSource _db;

        public OwnedDb(string connectionString, string dbName, NpgsqlDataSource db)
        {
            _connectionString = connectionString;
            _dbName = dbName;
            _db = db;
        }

        public string ConnectionString => _connectionString;

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<PostgreSQLSettings>(settings =>
            {
                settings.ConnectionString = ConnectionString;
                settings.AuthorizationDbPwd = "unused";
            });
        }

        public async ValueTask DisposeAsync()
        {
            await using var cmd = _db!.CreateCommand(/*strpsql*/$"DROP DATABASE IF EXISTS {_dbName};");

            await cmd.ExecuteNonQueryAsync();
        }
    }

    class TraceService : Yuniql.Extensibility.ITraceService
    {
        public static Yuniql.Extensibility.ITraceService Instance { get; } = new TraceService();

        /// <inheritdoc/>
        public bool IsDebugEnabled { get; set; } = false;

        /// <inheritdoc/>
        public bool IsTraceSensitiveData { get; set; } = false;

        /// <inheritdoc/>
        public bool IsTraceToFile { get; set; } = false;

        /// <inheritdoc/>
        public bool IsTraceToDirectory { get; set; } = false;

        /// <inheritdoc/>
        public string? TraceDirectory { get; set; }

        /// <inheritdoc/>
        public void Info(string message, object? payload = null)
        {
            var traceMessage = $"INF   {DateTime.UtcNow.ToString("o")}   {message}{Environment.NewLine}";
            Console.Write(traceMessage);
        }

        /// <inheritdoc/>
        public void Error(string message, object? payload = null)
        {
            var traceMessage = $"ERR   {DateTime.UtcNow.ToString("o")}   {message}{Environment.NewLine}";
            Console.Write(traceMessage);
        }

        /// <inheritdoc/>
        public void Debug(string message, object? payload = null)
        {
            if (IsDebugEnabled)
            {
                var traceMessage = $"DBG   {DateTime.UtcNow.ToString("o")}   {message}{Environment.NewLine}";
                Console.Write(traceMessage);
            }
        }

        /// <inheritdoc/>
        public void Success(string message, object? payload = null)
        {
            var traceMessage = $"INF   {DateTime.UtcNow.ToString("u")}   {message}{Environment.NewLine}";
            Console.Write(traceMessage);
        }

        /// <inheritdoc/>
        public void Warn(string message, object? payload = null)
        {
            var traceMessage = $"WRN   {DateTime.UtcNow.ToString("o")}   {message}{Environment.NewLine}";
            Console.Write(traceMessage);
        }
    }
}
