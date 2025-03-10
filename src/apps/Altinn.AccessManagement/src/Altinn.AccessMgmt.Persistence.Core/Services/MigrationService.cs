using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Executors;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.QueryBuilders;
using Altinn.AccessMgmt.Persistence.Core.Utilities;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <summary>
/// Service for handling migrations
/// </summary>
public class MigrationService
{
    private readonly DbDefinitionRegistry definitionRegistry;

    private readonly IDbExecutor executor;

    private readonly IOptions<AccessMgmtPersistenceOptions> options;

    /// <summary>
    /// Migration Services
    /// </summary>
    /// <param name="options">DbAccessConfig</param>
    /// <param name="definitionRegistry">DbDefinitionRegistry</param>
    /// <param name="executor">IDbExecutor</param>
    public MigrationService(IOptions<AccessMgmtPersistenceOptions> options, DbDefinitionRegistry definitionRegistry, IDbExecutor executor)
    {
        this.options = options;
        this.definitionRegistry = definitionRegistry;
        this.executor = executor;
        Migrations = new List<DbMigrationEntry>();
    }

    /*
    
    Start migration either by namespace or by type.
    After all types are added to the queue, run execute/print/export.

    TODO: new out methods

    //private void PrintScripts() { }
    //private void PrintScripts(Type type) { }
    //private void PrintScripts<T>() { }

    //private void ExportScripts<T>() { }
    //private void ExportScripts(Type type) { }
    //private void ExportScripts() { }

    */

    private Dictionary<Type, DbMigrationScriptCollection> Scripts { get; set; } = [];

    private bool HasInitialized { get; set; } = false;

    private async Task Init(CancellationToken cancellationToken = default)
    {
        if (HasInitialized)
        {
            return;
        }

        var config = this.options.Value;

        var defaultDefinition = new DbDefinition(typeof(string));

        // await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.BaseSchema};", new List<GenericParameter>(), cancellationToken);
        // await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.TranslationSchema};", new List<GenericParameter>(), cancellationToken);
        // await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.BaseHistorySchema};", new List<GenericParameter>(), cancellationToken);
        // await executor.ExecuteMigrationCommand($"CREATE SCHEMA IF NOT EXISTS {config.TranslationHistorySchema};", new List<GenericParameter>(), cancellationToken);
        var migrationTable = """
        CREATE TABLE IF NOT EXISTS dbo._migration (
        ObjectName text NOT NULL,
        Key text NOT NULL,
        At timestamptz NOT NULL,
        Status text NOT NULL,
        Script text NOT NULL,
        CollectionId text NOT NULL
        );
        """;

        await executor.ExecuteMigrationCommand(migrationTable, new List<GenericParameter>(), cancellationToken);

        Migrations = [.. await executor.ExecuteMigrationQuery<DbMigrationEntry>("SELECT * FROM dbo._migration")];

        HasInitialized = true;
    }

    private async Task Grants()
    {
        var config = this.options.Value;

        /*
        // Moved to terraform/bootstraping
        GRANT USAGE ON SCHEMA {config.BaseSchema} TO {config.DatabaseReadUser};
        GRANT USAGE ON SCHEMA {config.TranslationSchema} TO {config.DatabaseReadUser};
        GRANT USAGE ON SCHEMA {config.BaseHistorySchema} TO {config.DatabaseReadUser};
        GRANT USAGE ON SCHEMA {config.TranslationHistorySchema} TO {config.DatabaseReadUser};
        */

        //// TODO: Move to CREATE TABLE script

        string script = $"""
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.BaseSchema} TO {config.DatabaseReadUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.TranslationSchema} TO {config.DatabaseReadUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.BaseHistorySchema} TO {config.DatabaseReadUser};
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {config.TranslationHistorySchema} TO {config.DatabaseReadUser};
        """;

        await executor.ExecuteMigrationCommand(script);
    }

    /// <summary>
    /// Migrate
    /// </summary>
    /// <returns></returns>
    public async Task Migrate()
    {
        if (!HasInitialized)
        {
            await Init();
        }

        if (Scripts.Count == 0)
        {
            throw new Exception("Nothing to migrate. Remember to generate first.");
        }

        await ExecuteMigration();
        await Grants();
    }

    #region Generate

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

    /// <summary>
    /// Generate migration scripts for a type
    /// </summary>
    public void Generate(Type type, IDbQueryBuilder dbQueryBuilder)
    {
        Scripts.Add(type, dbQueryBuilder.GetMigrationScripts());
    }

    #endregion

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

        while (status.Values.Contains(false) && !failed.Any() && !cancellationToken.IsCancellationRequested)
        {
            foreach (var script in Scripts)
            {
                if (script.Key.Name == "Provider")
                {
                    Console.WriteLine("PROVIDER");
                }

                if (status[script.Key])
                {
                    continue;
                }

                if (retry[script.Key] > maxRetry)
                {
                    failed[script.Key] = "Max retry reached";
                    continue;
                }

                if (!script.Value.Dependencies.Any())
                {
                    try
                    {
                        await ExecuteMigration(script.Key, script.Value);
                        status[script.Key] = true;
                        retry[script.Key] = 0;
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR :: Migration '{script.Key.Name}' failed");
                        Console.WriteLine(ex.Message);
                        retry[script.Key]++;
                        continue;
                    }
                }

                bool needMigration = script.Value.Scripts.Keys.Any(a => NeedMigration(script.Key, a));

                if (!needMigration)
                {
                    status[script.Key] = true;
                    retry[script.Key] = 0;
                    continue;
                }

                // Check if all dependencies are migrated
                bool ready = script.Value.Dependencies.All(dep => status.ContainsKey(dep.Key) && status[dep.Key]);

                if (!ready)
                {
                    Console.WriteLine($"Migration '{script.Key.Name}' not ready");

                    Console.ForegroundColor = ConsoleColor.Red;
                    foreach (var dep in script.Value.Dependencies)
                    {
                        Console.WriteLine("Dep:" + dep.Key.Name);
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    foreach (var dep in status)
                    {
                        Console.WriteLine("Mig:" + dep.Key.Name + ":" + dep.Value);
                    }

                    Console.ForegroundColor = ConsoleColor.White;

                    retry[script.Key]++;
                    continue;
                }

                try
                {
                    await ExecuteMigration(script.Key, script.Value);
                    status[script.Key] = true;
                    retry[script.Key] = 0;
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Migration '{script.Key.Name}' failed: {ex.Message}");
                    retry[script.Key]++;
                    continue;
                }
            }

            Console.WriteLine("Migrationstatus:");
            foreach (var key in status.Keys)
            {
                if (status[key])
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine($"Migration '{key.Name}' status: {status[key]}");
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        Console.WriteLine("============FINAL==============");
        Console.WriteLine("Migrationstatus:");
        foreach (var key in status.Keys)
        {
            if (status[key])
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine($"Migration '{key.Name}' status: {status[key]}");
        }

        foreach (var f in failed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Migration '{f.Key.Name}' error: {f.Value}");
        }

        Console.ForegroundColor = ConsoleColor.White;
    }

    private async Task ExecuteMigration(Type type, DbMigrationScriptCollection collection)
    {
        var dbDefinition = definitionRegistry.TryGetDefinition(type) ?? throw new Exception($"GetOrAddDefinition for '{type.Name}' not found.");
        foreach (var script in collection.Scripts)
        {
            if (NeedMigration(type, script.Key))
            {
                if (script.Key.Contains("PK_"))
                {
                    Console.WriteLine("PK");
                    continue;
                }

                try
                {
                    await executor.ExecuteMigrationCommand(script.Value, new List<GenericParameter>());
                    await LogMigration(type, script.Key, script.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR :: Migration '{script.Key}' failed");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(script.Value);
                    throw;
                }
            }
        }
    }

    private List<DbMigrationEntry> Migrations { get; set; } = [];

    private Dictionary<Type, List<KeyValuePair<string, string>>> RetryQueue { get; set; } = [];

    private bool NeedMigration<T>(string key)
    {
        return NeedMigration(key: key, objectName: typeof(T).Name);
    }

    private bool NeedMigration(Type type, string key)
    {
        return NeedMigration(key: key, objectName: type.Name);
    }

    private bool NeedMigration(string key, string objectName)
    {
        if (Migrations == null)
        {
            throw new Exception("Migrations not initialize");
        }

        return !Migrations.Exists(t => t.ObjectName == objectName && t.Key == key);
    }

    private async Task LogMigration<T>(string key, string script)
    {
        await LogMigration(key: key, script: script, objectName: typeof(T).Name);
    }

    private async Task LogMigration(Type type, string key, string script)
    {
        await LogMigration(key: key, script: script, objectName: type.Name);
    }

    private async Task LogMigration(string key, string script, string objectName, CancellationToken cancellationToken = default)
    {
        var migrationEntry = new DbMigrationEntry
        {
            Key = key,
            At = DateTimeOffset.UtcNow,
            Status = "Executed",
            ObjectName = objectName,
            Script = script,
            CollectionId = "v1"
        };

        var parameters = new List<GenericParameter>
        {
            new GenericParameter("Key", key),
            new GenericParameter("At", DateTimeOffset.UtcNow),
            new GenericParameter("Status", "Executed"),
            new GenericParameter("ObjectName", objectName),
            new GenericParameter("Script", script),
            new GenericParameter("CollectionId", "v1")
        };

        await executor.ExecuteMigrationCommand("INSERT INTO dbo._migration (ObjectName, Key, At, Status, Script, CollectionId) VALUES(@ObjectName, @Key, @At, @Status, @Script, @CollectionId)", parameters, cancellationToken);
        Migrations.Add(migrationEntry);
        Console.WriteLine(key);
    }
}
