using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <summary>
/// Service for running schema migrations
/// </summary>
public class DbSchemaMigrationService
{
    private readonly IDbExecutor executor;
    private readonly IMigrationService migrationService;
    private readonly IOptions<AccessMgmtPersistenceOptions> options;
    private readonly DbDefinitionRegistry definitionRegistry;

    private Dictionary<Type, DbMigrationScriptCollection> Scripts { get; set; } = [];

    /// <summary>
    /// Migration Services
    /// </summary>
    /// <param name="options">DbAccessConfig</param>
    /// <param name="definitionRegistry">DbDefinitionRegistry</param>
    /// <param name="executor">IDbExecutor</param>
    /// <param name="migrationService">Keep track of migrations completed</param>
    public DbSchemaMigrationService(IOptions<AccessMgmtPersistenceOptions> options, DbDefinitionRegistry definitionRegistry, IDbExecutor executor, IMigrationService migrationService)
    {
        this.options = options;
        this.definitionRegistry = definitionRegistry;
        this.executor = executor;
        this.migrationService = migrationService;
    }

    private async Task PreMigration(CancellationToken cancellationToken = default)
    {
        var config = this.options.Value;
        await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.BaseSchema};", new List<GenericParameter>(), cancellationToken: cancellationToken);
        await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.TranslationSchema};", new List<GenericParameter>(), cancellationToken: cancellationToken);
        await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.BaseHistorySchema};", new List<GenericParameter>(), cancellationToken: cancellationToken);
        await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.TranslationHistorySchema};", new List<GenericParameter>(), cancellationToken: cancellationToken);
        await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.IngestSchema};", new List<GenericParameter>(), cancellationToken: cancellationToken);
        await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.ArchiveSchema};", new List<GenericParameter>(), cancellationToken: cancellationToken);
    }

    private async Task PostMigration(CancellationToken cancellationToken = default)
    {
        var config = this.options.Value;

        string schemaGrant = $"""
        GRANT USAGE ON SCHEMA {config.BaseSchema} TO {config.DatabaseAppUser};
        GRANT USAGE ON SCHEMA {config.TranslationSchema} TO {config.DatabaseAppUser};
        GRANT USAGE ON SCHEMA {config.BaseHistorySchema} TO {config.DatabaseAppUser};
        GRANT USAGE ON SCHEMA {config.TranslationHistorySchema} TO {config.DatabaseAppUser};
        GRANT USAGE ON SCHEMA {config.IngestSchema} TO {config.DatabaseAppUser};
        GRANT USAGE ON SCHEMA {config.ArchiveSchema} TO {config.DatabaseAppUser};
        """;
        await executor.ExecuteMigrationCommand(schemaGrant);

        string tableGrant = $"""
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.BaseSchema} TO {config.DatabaseAppUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.TranslationSchema} TO {config.DatabaseAppUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.BaseHistorySchema} TO {config.DatabaseAppUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.TranslationHistorySchema} TO {config.DatabaseAppUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.IngestSchema} TO {config.DatabaseAppUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.ArchiveSchema} TO {config.DatabaseAppUser};
        """;

        await executor.ExecuteMigrationCommand(tableGrant);
    }

    /// <summary>
    /// MigrateAll
    /// </summary>
    /// <returns></returns>
    public async Task MigrateAll(CancellationToken cancellationToken = default)
    {
        if (Scripts.Count == 0)
        {
            throw new Exception("Nothing to migrate. Remember to generate first.");
        }

        await PreMigration(cancellationToken: cancellationToken);
        await ExecuteMigration(cancellationToken: cancellationToken);
        await PostMigration(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Generate migration scripts for all types in a namespace
    /// </summary>
    public void GenerateAll()
    {
        var types = definitionRegistry.GetAllDefinitions().Select(t => t.ModelType).ToList();

        foreach (var type in types)
        {
            var q = definitionRegistry.GetQueryBuilder(type);
            Scripts.Add(type, q.GetMigrationScripts());
        }
    }

    private void Generate(Type type)
    {
        var q = definitionRegistry.GetQueryBuilder(type);
        Scripts.Add(type, q.GetMigrationScripts());
    }

    private async Task ExecuteMigration(int maxRetry = 10, CancellationToken cancellationToken = default)
    {
        var status = new Dictionary<Type, bool>();
        var retry = new Dictionary<Type, int>(); // Add reson ... For log
        var failed = new Dictionary<Type, string>();

        foreach (var key in Scripts.Keys)
        {
            status[key] = false;
            retry[key] = 0;
        }

        Console.WriteLine("Verifing migrations");
        var verificationFailures = 0;
        var verificationSuccess = 0;
        foreach (var scriptCollection in Scripts)
        {
            foreach (var script in scriptCollection.Value.Scripts)
            {
                var verified = migrationService.VerifyMigration(scriptCollection.Key.Name, script.Key, script.Value);
                if (!verified)
                {
                    verificationFailures++;
                    await migrationService.UndoMigration(scriptCollection.Key, script.Key);
                }
                else
                {
                    verificationSuccess++;
                }
            }
        }

        Console.WriteLine($"Verification complete: Success: {verificationSuccess} Failed: {verificationFailures}");

        while (status.Values.Contains(false) && !failed.Any() && !cancellationToken.IsCancellationRequested)
        {
            foreach (var script in Scripts)
            {
                if (status[script.Key])
                {
                    continue;
                }

                if (retry[script.Key] > maxRetry)
                {
                    failed[script.Key] = "Max retry reached";
                    continue;
                }

                bool needMigration = migrationService.NeedAnyMigration(script.Key, script.Value.Scripts.Select(t => t.Key).ToList());

                if (!needMigration)
                {
                    bool verified = true;
                    foreach (var s in script.Value.Scripts)
                    {
                        var res = migrationService.VerifyMigration(script.Key, s.Key, s.Value);
                        if (!res)
                        {
                            verified = false;
                            break;
                        }
                    }

                    if (verified)
                    {
                        status[script.Key] = true;
                        retry[script.Key] = 0;
                        continue;
                    }
                }

                if (!script.Value.Dependencies.Any())
                {
                    try
                    {
                        var res = await ExecuteMigration(script.Key, script.Value, retry[script.Key]);
                        if (res)
                        {
                            status[script.Key] = true;
                            retry[script.Key] = 0;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR :: Migration '{script.Key.Name}' failed");
                        Console.WriteLine(ex.Message);
                        retry[script.Key]++;
                        continue;
                    }
                }

                // Check if all dependencies are migrated
                bool ready = script.Value.Dependencies.All(dep => status.ContainsKey(dep.Key) && status[dep.Key]);

                if (!ready)
                {
                    retry[script.Key]++;
                    continue;
                }

                try
                {
                    var res = await ExecuteMigration(script.Key, script.Value, retry[script.Key]);
                    if (res)
                    {
                        status[script.Key] = true;
                        retry[script.Key] = 0;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    retry[script.Key]++;
                    Console.WriteLine($"Migration '{script.Key.Name}' failed, attempt: {retry[script.Key]}");
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
        }

        Console.WriteLine(string.Format("Migration complete: Success: {0} Failed: {1}", status.Count(t => t.Value), status.Count(t => !t.Value)));

        foreach (var f in failed)
        {
            Console.WriteLine($"Migration '{f.Key.Name}' error: {f.Value}");
        }
    }

    private async Task<bool> ExecuteMigration(Type type, DbMigrationScriptCollection collection, int retryAttempt)
    {
        var dbDefinition = definitionRegistry.TryGetDefinition(type) ?? throw new Exception($"GetOrAddDefinition for '{type.Name}' not found.");

        bool allGood = true;

        foreach (var script in collection.Scripts)
        {
            // Run all if any (temp)
            if (migrationService.NeedMigration(type, script.Key))
            {
                if (script.Key.Contains("PK_"))
                {
                    //// TODO: Hack ... Remove from scripts ...
                    continue;
                }

                try
                {
                    await executor.ExecuteMigrationCommand(script.Value, new List<GenericParameter>());
                    await migrationService.LogMigration(type, script.Key, script.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR :: Migration '{script.Key}' failed");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(script.Value);
                    allGood = false;
                }
            }
        }

        await migrationService.LogMigration(type, "Version", string.Empty, 1);

        return allGood;
    }
}
