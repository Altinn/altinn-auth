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

    private async Task MigrateFunctions()
    {
        var entityChildrenFunction = """
        create or replace function entitychildren(_id uuid) returns jsonb stable language sql as
        $$
        SELECT COALESCE(json_agg(compactentity(e.Id, false, true)) FILTER (WHERE e.Id IS NOT NULL), NULL)
        FROM dbo.Entity e
        WHERE e.ParentId = _id
        GROUP BY e.Id;
        $$;
        """;
        await executor.ExecuteMigrationCommand(entityChildrenFunction);

        var entityLookupValuesFunction = """
        create or replace function entityLookupValues(_id uuid) returns jsonb stable language sql as
        $$
        SELECT jsonb_object_agg(el.key, el.value)
        FROM dbo.EntityLookup el
        WHERE el.entityid = _id
        $$;
        """;
        await executor.ExecuteMigrationCommand(entityLookupValuesFunction);

        var roleLookupValuesFunction = """
        create or replace function roleLookupValues(_id uuid) returns jsonb stable language sql as
        $$
        SELECT jsonb_object_agg(rl.key, rl.value)
        FROM dbo.RoleLookup rl
        WHERE rl.roleid = _id
        $$;
        """;
        await executor.ExecuteMigrationCommand(roleLookupValuesFunction);

        var compactEntityFunction = """
            create or replace function compactentity(_id uuid, _include_children boolean DEFAULT true, _include_lookups boolean DEFAULT true) returns jsonb stable language sql as
            $$
            SELECT jsonb_build_object(
                'Id', e.Id,
                'Name', e.Name,
                'Type', et.Name,
                'Variant', ev.Name,
                'Parent', compactentity(e.parentid, false, true),
                'Children', CASE WHEN _include_children THEN entitychildren(e.id) ELSE NULL END,
                'KeyValues', CASE WHEN _include_lookups THEN entitylookupvalues(e.id) ELSE NULL END
                )
            FROM dbo.Entity e
            JOIN dbo.EntityType et ON e.TypeId = et.Id
            JOIN dbo.EntityVariant ev ON e.VariantId = ev.Id
            LEFT OUTER JOIN dbo.Entity as ce on e.Id = ce.ParentId
            LEFT OUTER JOIN dbo.EntityLookup as el on e.Id = el.entityid
            WHERE e.Id = _Id
            GROUP BY e.Id, e.Name, e.RefId, et.Name, ev.Name;
            $$;
            """;
        await executor.ExecuteMigrationCommand(compactEntityFunction);

        var compactRoleFunction = """
            create or replace function public.compactRole(_id uuid) returns jsonb stable language sql as
            $$
            SELECT jsonb_build_object(
                'Id', r.Id,
                'Code', r.Code,
                'Children', COALESCE(
                                json_agg(json_build_object('Id', rmr.Id, 'Value', rmr.Code, 'Children', null))
                                FILTER (WHERE rmr.Id IS NOT NULL), NULL)
            )
            FROM dbo.role r
            left outer join dbo.RoleMap as rm on rm.HasRoleId = r.Id
            left outer join dbo.Role as rmr on rm.GetRoleId = rmr.Id
            WHERE r.id = _Id
            group by r.Id, r.Name;
            $$;
            """;
        await executor.ExecuteMigrationCommand(compactRoleFunction);

        var compactPackageFunction = """
            create or replace function compactpackage(_id uuid) returns jsonb stable language sql as
            $$
            select jsonb_build_object('Id', p.Id,'Urn', p.Urn, 'AreaId', p.AreaId)
            from dbo.Package as p
            where p.id = _id;
            $$;
            """;
        await executor.ExecuteMigrationCommand(compactPackageFunction);

        var compactResourceFunction = """
            create or replace function compactresource(_id uuid) returns jsonb stable language sql as
            $$
            select jsonb_build_object('Id', r.Id,'Value', r.RefId)
            from dbo.Resource as r
            where r.id = _id;
            $$;
            """;
        await executor.ExecuteMigrationCommand(compactResourceFunction);

        var nameFunctions = """
        create or replace function namerole(_id uuid) returns text stable language sql as $$ select code from dbo.role where id = _id; $$;
        create or replace function nameentity(_id uuid) returns text stable language sql as $$ select e.name || ' (' || ev.name || ')' from dbo.entity as e inner join dbo.entityvariant as ev on e.variantid = ev.id where e.id = _id; $$;
        create or replace function namepackage(_id uuid) returns text stable language sql as $$ select name from dbo.package where id = _id; $$;
        create or replace function nameassignment(_id uuid) returns text stable language sql as $$ select nameentity(a.fromid) || ' - ' || namerole(a.roleid) || ' - '  || nameentity(a.toid) from dbo.assignment as a where id = _id; $$;
        create or replace function namedelegation(_id uuid) returns text stable language sql as $$ select nameassignment(d.fromid) || ' | ' || nameentity(d.facilitatorid) || ' | ' || nameassignment(d.toid) from dbo.delegation as d where id = _id; $$;
        """;

        await executor.ExecuteMigrationCommand(nameFunctions);
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
        await MigrateFunctions();
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
