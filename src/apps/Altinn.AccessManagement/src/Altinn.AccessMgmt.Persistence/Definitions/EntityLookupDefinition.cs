using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class EntityLookupDefinition : BaseDbDefinition<EntityLookup>, IDbDefinition
{
    /// <inheritdoc/>
    public EntityLookupDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<EntityLookup>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.EntityId);
            def.RegisterProperty(t => t.Key);
            def.RegisterProperty(t => t.Value);
            def.RegisterProperty(t => t.IsProtected, defaultValue: "false");

            def.RegisterExtendedProperty<ExtEntityLookup, Entity>(t => t.EntityId, t => t.Id, t => t.Entity, cascadeDelete: true);
            def.RegisterUniqueConstraint([t => t.EntityId, t => t.Key], includedProperties: [t => t.Value, t => t.Id]);

            def.AddManualPostMigrationScript(1, PostMigrationScript_IsProtectedInit());
        });
    }

    private string PostMigrationScript_IsProtectedInit()
    {
        return """
        DO
        $$
        BEGIN
        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'dbo' AND table_name = 'entitylookup') THEN
            CREATE TEMP TABLE session_audit_context ON COMMIT DROP AS
            SELECT 
              '3296007F-F9EA-4BD0-B6A6-C8462D54633A'::uuid AS changed_by,
              '3296007F-F9EA-4BD0-B6A6-C8462D54633A'::uuid AS changed_by_system,
              '3296007F-F9EA-4BD0-B6A6-C8462D54633A'::text AS change_operation_id;
        
              UPDATE dbo.entitylookup SET IsProtected = true WHERE Key = 'PersonIdentifier';
          END IF;
        END
        $$;
        """;
    }
}
