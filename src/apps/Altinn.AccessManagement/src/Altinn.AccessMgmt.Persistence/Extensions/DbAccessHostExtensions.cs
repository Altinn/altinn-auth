using System.Reflection;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Executors;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Services;
using Altinn.AccessMgmt.Persistence.Core.Utilities;
using Altinn.AccessMgmt.Persistence.Core.Utilities.Search;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Data.Mock;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.Authorization.Host.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Persistence.Extensions;

/// <summary>
/// Provides extension methods for configuring database access services.
/// </summary>
public static partial class DbAccessHostExtensions
{
    /// <summary>
    /// Logger instance for logging database configuration messages.
    /// </summary>
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(DbAccessHostExtensions));

    /// <summary>
    /// Adds Db Audit Middleware
    /// </summary>
    public static IApplicationBuilder UseDbAudit(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<AuditService>();
        return builder;
    }

    /// <summary>
    /// Adds database access services to the application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configureOptions">Action to configure database access options.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostApplicationBuilder AddAccessMgmtDb(this IHostApplicationBuilder builder, Action<AccessMgmtPersistenceOptions> configureOptions)
    {
        var options = new AccessMgmtPersistenceOptions();
        configureOptions?.Invoke(options);

        Log.ConfigureDbType(Logger, options.DbType);
        builder.ConfigureAccessMgmtDb(options.DbType)();

        // Add repository implementations dynamically
        var assembly = Assembly.GetExecutingAssembly();
        var repositoryTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "Altinn.AccessMgmt.Persistence.Repositories" && t.Name.EndsWith("Repository"))
            .ToList();

        foreach (var repoType in repositoryTypes)
        {
            var interfaceType = repoType.GetInterfaces().FirstOrDefault(i => i.Name == "I" + repoType.Name);
            if (interfaceType != null)
            {
                builder.Services.AddSingleton(interfaceType, repoType);
            }
        }

        builder.Services.AddSingleton<AuditService>();
        builder.Services.AddSingleton<IDbAudit, AuditFactory>();
        builder.Services.AddSingleton<IDbAuditService, AuditFactory>();

        builder.Services.AddSingleton<DbDefinitionRegistry>();
        builder.Services.AddSingleton(typeof(ISearchCache<>), typeof(SearchCache<>));

        builder.Services.AddSingleton<IRelationService, RelationService>();
        builder.Services.AddSingleton<IConnectionService, ConnectionService>();
        builder.Services.AddSingleton<IAssignmentService, AssignmentService>();
        builder.Services.AddSingleton<IDelegationService, DelegationService>();
        builder.Services.AddSingleton<IPackageService, PackageService>();
        builder.Services.AddSingleton<IRoleService, RoleService>();
        builder.Services.AddSingleton<IPartyService, PartyService>();
        builder.Services.AddSingleton<IStatusService, StatusService>();
        builder.Services.AddSingleton<IAuthorizedPartyRepoService, AuthorizedPartyRepoService>();

        builder.Services.AddSingleton<IIngestService, PostgresIngestService>();
        builder.Services.AddSingleton<IDbExecutor, PostgresDbExecutor>();
        builder.Services.AddSingleton<IDbConverter, DbConverter>();
        builder.Services.AddSingleton<IMigrationService, SqlMigrationService>();
        builder.Services.AddSingleton<DbDefinitionRegistry>();
        builder.Services.AddSingleton<DbSchemaMigrationService>();
        builder.Services.AddSingleton<DbDataMigrationService>();
        builder.Services.AddSingleton<MockDataService>();

        // Core interfaces & implementations
        builder.Services.AddSingleton<IAmPartyRepository, AMPartyService>();

        builder.Services.Add(Marker.ServiceDescriptor);

        return builder;
    }

    /// <summary>
    /// Configures database services based on the specified database type.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="dbType">The type of database to configure.</param>
    /// <returns>An action to configure the database services.</returns>
    private static Action ConfigureAccessMgmtDb(this IHostApplicationBuilder builder, MgmtDbType dbType) => dbType switch
    {
        MgmtDbType.Postgres => () =>
        {
            builder.Services.AddSingleton<IDbConverter, DbConverter>();
            builder.Services.AddSingleton<IDbExecutor, PostgresDbExecutor>();
        }
        ,
        MgmtDbType.MSSQL => () =>
        {
            builder.Services.AddSingleton<IDbConverter, DbConverter>(); // TODO: Add MSSQL converter
            builder.Services.AddSingleton<IDbExecutor, MssqlDbExecutor>();
        }
        ,
        _ => () => throw new InvalidOperationException($"Unknown database type: {dbType}"),
    };

    /// <summary>
    /// Initializes and applies database migrations and data ingestion processes.
    /// </summary>
    /// <param name="host">The application host.</param>
    /// <param name="generateBasicData">Optional flag to generate basic data for test</param>
    /// <returns>The updated host after applying database changes.</returns>
    public static async Task<IHost> UseAccessMgmtDb(this IHost host, bool? generateBasicData = false)
    {
        // Make sure migration don't run if DB is not enabled
        if (host.Services.GetService(Marker.Type) == null)
        {
            Log.DbNotEnabled(Logger);
            return host;
        }

        // Add definitions to the database definition registry
        DefineAllModels(host.Services);

        var migration = host.Services.GetRequiredService<DbSchemaMigrationService>();
        migration.GenerateAll();
        await migration.MigrateAll();

        var dbIngest = host.Services.GetRequiredService<DbDataMigrationService>();
        await dbIngest.IngestAll();

        /*
        //// TODO: Add FeatureFlag
        // var mockService = host.Services.GetRequiredService<MockDataService>();
        // await mockService.GenerateBasicData();
        // await mockService.GeneratePackageResources();
        // await mockService.SystemUserClientDelegation();
        */
        if (generateBasicData == true)
        {
            var mockService = host.Services.GetRequiredService<MockDataService>();
            await mockService.GenerateBasicData();

            // await mockService.GeneratePackageResources();
        }

        return host;
    }

    /// <summary>
    /// Defines all database models and registers them in the database definition registry.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="definitionNamespace">Optional namespace to filter the definitions.</param>
    private static void DefineAllModels(IServiceProvider serviceProvider, string definitionNamespace = null)
    {
        var assemblies = string.IsNullOrEmpty(definitionNamespace)
            ? AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name!.StartsWith(Assembly.GetExecutingAssembly().GetName().Name!))
            : [Assembly.Load(definitionNamespace)];

        var definitions = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IDbDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => (IDbDefinition)ActivatorUtilities.CreateInstance(serviceProvider, t)!)
            .ToList();

        definitions.ForEach(def => def.Define());
    }

    private sealed class Marker
    {
        public static readonly Type Type = typeof(Marker);

        public static readonly ServiceDescriptor ServiceDescriptor = ServiceDescriptor.Singleton<Marker, Marker>();
    }

    /// <summary>
    /// Logging utility for database configuration messages.
    /// </summary>
    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Configuring {dbtype} database for access management")]
        internal static partial void ConfigureDbType(ILogger logger, MgmtDbType dbtype);

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = $"Database is not enabled make sure it's initialized by calling IHostApplicationBuilder.AddAccessMgmtDb.")]
        internal static partial void DbNotEnabled(ILogger logger);
    }
}
