using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.Services;

/// <inheritdoc/>
public class SqlMigrationService(IDbExecutor executor) : IMigrationService
{
    private List<DbMigrationEntry> Migrations { get; set; } = new();

    private bool HasInitialized { get; set; } = false;

    private readonly IDbExecutor executor = executor;

    /// <inheritdoc />
    public async Task Init(CancellationToken cancellationToken = default)
    {
        if (!HasInitialized)
        {
            var migrationTable = """
            CREATE TABLE IF NOT EXISTS dbo._dbmigration (
            ObjectName text NOT NULL,
            Key text NOT NULL,
            Version int NOT NULL,
            Script text NOT NULL,
            CompletedAt timestamptz NULL
            );
            """;

            await executor.ExecuteMigrationCommand(migrationTable, new List<GenericParameter>(), cancellationToken);

            HasInitialized = true;
        }

        Migrations = [.. await executor.ExecuteMigrationQuery<DbMigrationEntry>("SELECT * FROM dbo._dbmigration")];
    }

    /// <inheritdoc /> 
    public bool NeedAnyMigration(Dictionary<Type, List<string>> typeKeys)
    {
        foreach (var typeKey in typeKeys)
        {
            if (NeedAnyMigration(typeKey.Key, typeKey.Value))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc /> 
    public bool NeedAnyMigration(Type type, List<string> keys)
    {
        foreach (var key in keys)
        {
            if (NeedMigration(type, key))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public bool NeedMigration<T>(string key, int version = 1)
    {
        return NeedMigration(objectName: typeof(T).Name, key: key, version: version);
    }

    /// <inheritdoc/>
    public bool NeedMigration(Type type, string key, int version = 1)
    {
        return NeedMigration(objectName: type.Name, key: key, version: version);
    }

    /// <inheritdoc/>
    public bool NeedMigration(string objectName, string key, int version = 1)
    {
        if (Migrations == null || !Migrations.Any())
        {
            Init().Wait();
        }

        if (Migrations == null || !Migrations.Any())
        {
            return true;
        }

        return !Migrations.Exists(t => t.ObjectName == objectName && t.Key == key && t.Version == version);
    }

    /// <inheritdoc /> 
    public bool VerifyMigration(Type type, string key, string script, int version = 1)
    {
        return VerifyMigration(type.Name, key, script, version);
    }

    /// <inheritdoc /> 
    public bool VerifyMigration(string objectName, string key, string script, int version = 1)
    {
        if (Migrations == null || !Migrations.Any())
        {
            Init().Wait();
        }

        var migration = Migrations.FirstOrDefault(t => t.ObjectName == objectName && t.Key == key && t.Version == version);
        if (migration != null)
        {
            if (!migration.Script.Equals(script))
            {
                Console.WriteLine($"WARNING: Generated script does not match migration script. '{objectName}':'{key}':'{version}'");
                Console.WriteLine($"Migrated");
                Console.WriteLine(migration.Script);
                Console.WriteLine($"Generated");
                Console.WriteLine(script);
                Console.WriteLine("=========");

                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task UndoMigration(Type type, string key, int version = 1, CancellationToken cancellationToken = default)
    {
        var migrationEntry = new DbMigrationEntry
        {
            ObjectName = type.Name,
            Key = key,
            Version = version,
            Script = " ",
            CompletedAt = DateTimeOffset.UtcNow
        };

        var parameters = new List<GenericParameter>
        {
            new GenericParameter("ObjectName", type.Name),
            new GenericParameter("Key", key),
            new GenericParameter("Version", version),
            new GenericParameter("Script", " "),
            new GenericParameter("CompletedAt", DateTimeOffset.UtcNow)
        };

        await executor.ExecuteMigrationCommand("DELETE FROM dbo._dbmigration WHERE ObjectName = @ObjectName AND Key = @Key AND Version = @Version", parameters, cancellationToken);
        Migrations.RemoveAll(t => t.ObjectName == migrationEntry.ObjectName && t.Key == migrationEntry.Key && t.Version == migrationEntry.Version);
        Console.WriteLine("UNDO: " + key);
    }

    /// <inheritdoc/>
    public async Task LogMigration<T>(string key, string script, int version = 1, CancellationToken cancellationToken = default)
    {
        await LogMigration(objectName: typeof(T).Name, key: key, script: script, version: version, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task LogMigration(Type type, string key, string script, int version = 1, CancellationToken cancellationToken = default)
    {
        await LogMigration(objectName: type.Name, key: key, script: script, version: version, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task LogMigration(string objectName, string key, string script, int version = 1, CancellationToken cancellationToken = default)
    {
        var migrationEntry = new DbMigrationEntry
        {
            ObjectName = objectName,
            Key = key,
            Version = version,
            Script = script,
            CompletedAt = DateTimeOffset.UtcNow
        };

        var parameters = new List<GenericParameter>
        {
            new GenericParameter("ObjectName", objectName),
            new GenericParameter("Key", key),
            new GenericParameter("Version", version),
            new GenericParameter("Script", script),
            new GenericParameter("CompletedAt", DateTimeOffset.UtcNow)
        };

        await executor.ExecuteMigrationCommand("INSERT INTO dbo._dbmigration (ObjectName, Key, Version, Script, CompletedAt) VALUES(@ObjectName, @Key, @Version, @Script, @CompletedAt)", parameters, cancellationToken);
        Migrations.Add(migrationEntry);
        Console.WriteLine(key);
    }
}
