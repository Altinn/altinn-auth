using System.Text;
using Microsoft.EntityFrameworkCore;
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

        var entityType = model?.GetEntityTypes().FirstOrDefault(et =>
            et.GetTableName() == operation.Name &&
            et.GetSchema() == operation.Schema);

        if (entityType?.FindAnnotation("EnableAudit")?.Value as bool? == true)
        {
            var columns = GetDataColumnNames(operation, model);
            
            builder.AppendLine(GenerateAuditInsertFunctionAndTrigger(operation.Schema, operation.Name, columns));
            builder.EndCommand();

            builder.AppendLine(GenerateAuditUpdateFunctionAndTrigger(operation.Schema, operation.Name, columns));
            builder.EndCommand();

            builder.AppendLine(GenerateAuditDeleteFunctionAndTrigger(operation.Schema, operation.Name, columns));
            builder.EndCommand();
        }

        if (entityType?.FindAnnotation("EnableTranslation")?.Value as bool? == true)
        {
            // Find all properties with annotation "Translate"
            // Moved to TranslationService
        }
    }

    private static List<string> GetDataColumnNames(CreateTableOperation op, IModel? model)
    {
        var cols = new List<string>();
        if (model is null)
        {
            return cols;
        }

        var et = model.GetEntityTypes()
            .FirstOrDefault(x => x.GetTableName() == op.Name && x.GetSchema() == op.Schema);

        if (et is null)
        {
            return cols;
        }

        var storeObject = StoreObjectIdentifier.Table(op.Name, op.Schema);

        cols = et.GetProperties()
            .Select(p => p.GetColumnName(storeObject))
            .Where(n => n != null && !n.StartsWith("audit_", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList()!;

        // Fallback til operation.Columns hvis noe ikke var mappet:
        if (cols.Count == 0)
        {
            cols = op.Columns
                .Select(c => c.Name)
                .Where(n => !n.StartsWith("audit_", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();
        }

        return cols;
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
