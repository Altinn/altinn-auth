using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RolePackageDefinition : BaseDbDefinition<RolePackage>, IDbDefinition
{
    /// <inheritdoc/>
    public RolePackageDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<RolePackage>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.RoleId);
            def.RegisterProperty(t => t.PackageId);
            def.RegisterProperty(t => t.HasAccess);
            def.RegisterProperty(t => t.CanDelegate);
            def.RegisterProperty(t => t.EntityVariantId!, nullable: true);

            def.RegisterExtendedProperty<ExtRolePackage, Role>(t => t.RoleId, t => t.Id, t => t.Role, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtRolePackage, Package>(t => t.PackageId, t => t.Id, t => t.Package, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtRolePackage, EntityVariant>(t => t.EntityVariantId!, t => t.Id, t => t.EntityVariant!, cascadeDelete: false, optional: true);

            def.RegisterUniqueConstraint([t => t.RoleId, t => t.PackageId], nullableProperties: [t => t.EntityVariantId]);

            def.AddManualPreMigrationScript(1, GetPreMigrationScript_UniqueConstraint());
            def.AddManualPreMigrationScript(2, GetPreMigrationScript_RemoveDuplicates());
        });
    }

    private string GetPreMigrationScript_UniqueConstraint()
    {
        return """
            DO
            $$
            BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'dbo' AND table_name = 'rolepackage') THEN
                ALTER TABLE dbo.rolepackage DROP CONSTRAINT IF EXISTS uc_rolepackage_roleid_packageid_entityvariantid;
              END IF;
            END
            $$;
            """;
    }

    private string GetPreMigrationScript_RemoveDuplicates()
    {
        return """
            DO
            $$
            BEGIN
                CREATE TEMP TABLE session_audit_context ON COMMIT DROP AS
                SELECT 
                  '3296007F-F9EA-4BD0-B6A6-C8462D54633A'::uuid AS changed_by,
                  '3296007F-F9EA-4BD0-B6A6-C8462D54633A'::uuid AS changed_by_system,
                  '3296007F-F9EA-4BD0-B6A6-C8462D54633A'::text AS change_operation_id;

                WITH dups AS (
                  SELECT id, ROW_NUMBER() OVER (PARTITION BY roleid, packageid, entityvariantid ORDER BY id) AS rn
                  FROM dbo.rolepackage
                )
                DELETE FROM dbo.rolepackage AS m
                USING dups AS d
                WHERE m.id = d.id AND d.rn > 1;
            END
            $$;
            """;
    }
}
