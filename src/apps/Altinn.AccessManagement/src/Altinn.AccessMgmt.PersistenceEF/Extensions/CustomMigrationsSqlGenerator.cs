using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public class CustomMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    public CustomMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        INpgsqlSingletonOptions npgsqlOptions)
        : base(dependencies, npgsqlOptions) { }

    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        base.Generate(operation, model, builder, terminate);
        builder.EndCommand();

        var effectiveModel = GetEffectiveModel(model);
        var entityType = FindEntityType(effectiveModel, operation.Name, operation.Schema);
        var columns = GetDataColumnNames(operation);

        GenerateScripts(entityType, effectiveModel, builder, columns);
    }

    protected override void Generate(AddColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        base.Generate(operation, model, builder, terminate);
        builder.EndCommand();

        var effectiveModel = GetEffectiveModel(model);
        var entityType = FindEntityType(effectiveModel, operation.Table, operation.Schema);

        if (entityType is null)
        {
            return;
        }

        var columns = GetDataColumnNames(
            effectiveModel,
            operation.Table,
            operation.Schema,
            addedColumn: operation.Name);

        GenerateScripts(entityType, effectiveModel, builder, columns);
    }

    protected override void Generate(DropColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        base.Generate(operation, model, builder, terminate);
        builder.EndCommand();

        var effectiveModel = GetEffectiveModel(model);
        var entityType = FindEntityType(effectiveModel, operation.Table, operation.Schema);

        if (entityType is null)
        {
            return;
        }

        var columns = GetDataColumnNames(
            effectiveModel,
            operation.Table,
            operation.Schema,
            removedColumn: operation.Name);

        GenerateScripts(entityType, effectiveModel, builder, columns);
    }

    protected override void Generate(AlterColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        base.Generate(operation, model, builder);
        builder.EndCommand();

        var effectiveModel = GetEffectiveModel(model);
        var entityType = FindEntityType(effectiveModel, operation.Table, operation.Schema);

        if (entityType is null)
        {
            return;
        }

        var columns = GetDataColumnNames(effectiveModel, operation.Table, operation.Schema);

        GenerateScripts(entityType, effectiveModel, builder, columns);
    }

    protected override void Generate(RenameColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        base.Generate(operation, model, builder);
        builder.EndCommand();

        var effectiveModel = GetEffectiveModel(model);
        var entityType = FindEntityType(effectiveModel, operation.Table, operation.Schema);

        if (entityType is null)
        {
            return;
        }

        var columns = GetDataColumnNames(
            effectiveModel,
            operation.Table,
            operation.Schema,
            addedColumn: operation.NewName,
            removedColumn: operation.Name);

        GenerateScripts(entityType, effectiveModel, builder, columns);
    }

    // Viktig for annotation-changes på entity/table
    protected override void Generate(AlterTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        base.Generate(operation, model, builder);
        builder.EndCommand();

        var effectiveModel = GetEffectiveModel(model);
        var entityType = FindEntityType(effectiveModel, operation.Name, operation.Schema);

        if (entityType is null)
        {
            return;
        }

        var columns = GetDataColumnNames(effectiveModel, operation.Name, operation.Schema);

        GenerateScripts(entityType, effectiveModel, builder, columns);
    }

    // Viktig for global “audit version” / database-annotations
    protected override void Generate(AlterDatabaseOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        base.Generate(operation, model, builder);
        builder.EndCommand();

        var effectiveModel = GetEffectiveModel(model);

        foreach (var entityType in effectiveModel.GetEntityTypes())
        {
            if (entityType.FindAnnotation(AuditExtensions.AnnotationName) is null)
            {
                continue;
            }

            var table = entityType.GetTableName();
            var schema = entityType.GetSchema();

            if (string.IsNullOrWhiteSpace(table))
            {
                continue;
            }

            var columns = GetDataColumnNames(effectiveModel, table!, schema);

            GenerateScripts(entityType, effectiveModel, builder, columns);
        }
    }

    private IModel GetEffectiveModel(IModel? model)
    {
        // 1) Hvis EF sender inn en reell app-model, bruk den
        if (model is not null && IsApplicationModel(model))
        {
            return model;
        }

        // 2) Stabil kilde for app-modell i både CLI og runtime
        var migrationsAssembly = Dependencies.CurrentContext.Context.GetService<IMigrationsAssembly>();
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;

        if (snapshotModel is not null && IsApplicationModel(snapshotModel))
        {
            return snapshotModel;
        }

        // 3) Siste fallback (kan være HistoryRow-only)
        return Dependencies.CurrentContext.Context.Model;
    }

    private static bool IsApplicationModel(IModel model)
    {
        // HistoryRow-only => kun __EFMigrationsHistory
        return model.GetEntityTypes().Any(et =>
            !string.Equals(et.GetTableName(), "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase));
    }

    private static IEntityType? FindEntityType(IModel model, string table, string? schema)
    {
        return model.GetEntityTypes()
            .FirstOrDefault(et =>
                et.GetTableName() == table &&
                et.GetSchema() == schema);
    }

    private void GenerateScripts(IEntityType? entityType, IModel model, MigrationCommandListBuilder builder, List<string> columns)
    {
        if (entityType is null)
        {
            return;
        }

        if (entityType.FindAnnotation(AuditExtensions.AnnotationName) is null)
        {
            return;
        }

        var schema = entityType.GetSchema();
        var table = entityType.GetTableName();

        builder.AppendLine(GenerateAuditInsertFunctionAndTrigger(schema!, table!, columns));
        builder.EndCommand();

        builder.AppendLine(GenerateAuditUpdateFunctionAndTrigger(schema!, table!, columns));
        builder.EndCommand();

        builder.AppendLine(GenerateAuditDeleteFunctionAndTrigger(schema!, table!, columns));
        builder.EndCommand();
    }

    private static List<string> GetDataColumnNames(CreateTableOperation operation)
    {
        return operation.Columns
            .Select(c => c.Name)
            .Where(n => !n.StartsWith("audit_", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(n => n)
            .ToList();
    }

    private static List<string> GetDataColumnNames(
        IModel model,
        string table,
        string? schema,
        string? addedColumn = null,
        string? removedColumn = null)
    {
        var entityType = FindEntityType(model, table, schema);

        if (entityType is null)
        {
            return new();
        }

        var storeObject = StoreObjectIdentifier.Table(table, schema);

        var columns = entityType.GetProperties()
            .Select(p => p.GetColumnName(storeObject))
            .Where(n => n is not null && !n.StartsWith("audit_", StringComparison.OrdinalIgnoreCase))
            .ToList()!;

        if (!string.IsNullOrWhiteSpace(addedColumn) && !columns.Contains(addedColumn))
        {
            columns.Add(addedColumn);
        }

        if (!string.IsNullOrWhiteSpace(removedColumn))
        {
            columns.Remove(removedColumn);
        }

        return columns
            .Distinct()
            .OrderBy(n => n)
            .ToList();
    }

    private string GenerateAuditInsertFunctionAndTrigger(string schema, string name, List<string> columns)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE OR REPLACE FUNCTION {schema}.audit_{name}_insert_fn() returns TRIGGER language plpgsql AS $$");
        sb.AppendLine("BEGIN");
        sb.AppendLine("DECLARE");
        sb.AppendLine("changed_by UUID;");
        sb.AppendLine("changed_by_system UUID;");
        sb.AppendLine("change_operation_id text;");
        sb.AppendLine("BEGIN");
        sb.AppendLine("SELECT current_setting('app.changed_by', false) INTO changed_by;");
        sb.AppendLine("SELECT current_setting('app.changed_by_system', false) INTO changed_by_system;");
        sb.AppendLine("SELECT current_setting('app.change_operation_id', false) INTO change_operation_id;");
        sb.AppendLine("IF NEW.audit_changedby IS NULL THEN NEW.audit_changedby := changed_by; END IF;");
        sb.AppendLine("IF NEW.audit_changedbysystem IS NULL THEN NEW.audit_changedbysystem := changed_by_system; END IF;");
        sb.AppendLine("IF NEW.audit_changeoperation IS NULL THEN NEW.audit_changeoperation := change_operation_id; END IF;");
        sb.AppendLine("IF NEW.audit_validfrom IS NULL THEN NEW.audit_validfrom := now(); END IF;");
        sb.AppendLine("RETURN NEW;");
        sb.AppendLine("END;");
        sb.AppendLine("END;");
        sb.AppendLine("$$;");

        sb.AppendLine($"DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_{name}_insert_trg' AND t.tgrelid = to_regclass('{schema}.{name}')) THEN");
        sb.AppendLine($"CREATE OR REPLACE TRIGGER audit_{name}_insert_trg BEFORE INSERT OR UPDATE ON {schema}.{name}");
        sb.AppendLine($"FOR EACH ROW EXECUTE FUNCTION {schema}.audit_{name}_insert_fn();");
        sb.AppendLine($"END IF; END $$;");

        return sb.ToString();
    }

    private string GenerateAuditUpdateFunctionAndTrigger(string schema, string name, List<string> columns)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE OR REPLACE FUNCTION {schema}.audit_{name}_update_fn()");
        sb.AppendLine("RETURNS TRIGGER AS $$");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"INSERT INTO {Utils.BaseConfiguration.AuditSchema}.audit{name} (");
        sb.AppendLine($"{string.Join(',', columns)},");
        sb.AppendLine("audit_validfrom, audit_validto,");
        sb.AppendLine("audit_changedby, audit_changedbysystem, audit_changeoperation");
        sb.AppendLine(") VALUES (");
        sb.AppendLine($"{string.Join(',', columns.Select(t => "OLD." + t))},");
        sb.AppendLine("OLD.audit_validfrom, now(),");
        sb.AppendLine("OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation");
        sb.AppendLine(");");
        sb.AppendLine("RETURN NEW;");
        sb.AppendLine("END;");
        sb.AppendLine("$$ LANGUAGE plpgsql;");

        sb.AppendLine($"DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_{name}_update_trg' AND t.tgrelid = to_regclass('{schema}.{name}')) THEN");
        sb.AppendLine($"CREATE OR REPLACE TRIGGER audit_{name}_update_trg AFTER UPDATE ON {schema}.{name}");
        sb.AppendLine($"FOR EACH ROW EXECUTE FUNCTION {schema}.audit_{name}_update_fn();");
        sb.AppendLine($"END IF; END $$;");

        return sb.ToString();
    }

    private string GenerateAuditDeleteFunctionAndTrigger(string schema, string name, List<string> columns)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE OR REPLACE FUNCTION {schema}.audit_{name}_delete_fn()");
        sb.AppendLine("RETURNS TRIGGER AS $$");
        sb.AppendLine("DECLARE ctx RECORD;");
        sb.AppendLine("BEGIN");
        sb.AppendLine("SELECT * INTO ctx FROM session_audit_context LIMIT 1;");
        sb.AppendLine($"INSERT INTO {Utils.BaseConfiguration.AuditSchema}.audit{name} (");
        sb.AppendLine($"{string.Join(',', columns)},");
        sb.AppendLine("audit_validfrom, audit_validto,");
        sb.AppendLine("audit_changedby, audit_changedbysystem, audit_changeoperation,");
        sb.AppendLine("audit_deletedby, audit_deletedbysystem, audit_deleteoperation");
        sb.AppendLine(") VALUES (");
        sb.AppendLine($"{string.Join(',', columns.Select(t => "OLD." + t))},");
        sb.AppendLine("OLD.audit_validfrom, now(),");
        sb.AppendLine("OLD.audit_changedby, OLD.audit_changedbysystem, OLD.audit_changeoperation,");
        sb.AppendLine("ctx.changed_by, ctx.changed_by_system, ctx.change_operation_id");
        sb.AppendLine(");");
        sb.AppendLine("RETURN OLD;");
        sb.AppendLine("END;");
        sb.AppendLine("$$ LANGUAGE plpgsql;");

        sb.AppendLine($"DO $$ BEGIN IF NOT EXISTS (SELECT * FROM pg_trigger t WHERE t.tgname ILIKE 'audit_{name}_delete_trg' AND t.tgrelid = to_regclass('{schema}.{name}')) THEN");
        sb.AppendLine($"CREATE OR REPLACE TRIGGER audit_{name}_delete_trg AFTER DELETE ON {schema}.{name}");
        sb.AppendLine($"FOR EACH ROW EXECUTE FUNCTION {schema}.audit_{name}_delete_fn();");
        sb.AppendLine($"END IF; END $$;");

        return sb.ToString();
    }

}
