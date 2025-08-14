using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RoleLookupDefinition : BaseDbDefinition<RoleLookup>, IDbDefinition
{
    /// <inheritdoc/>
    public RoleLookupDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<RoleLookup>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.RoleId);
            def.RegisterProperty(t => t.Key);
            def.RegisterProperty(t => t.Value);

            def.RegisterExtendedProperty<ExtRoleLookup, Role>(t => t.RoleId, t => t.Id, t => t.Role, cascadeDelete: true);
            def.RegisterUniqueConstraint([t => t.RoleId, t => t.Key], includedProperties: [t => t.Value, t => t.Id]);

            def.AddManualPreMigrationScript(1, GetPreMigrationScript_RemoveRoleLookupKeyERCode());
        });
    }

    private string GetPreMigrationScript_RemoveRoleLookupKeyERCode()
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

                DELETE FROM dbo.rolelookup
                WHERE key = 'ERCode';
            END
            $$;
            """;
    }
}
