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
        DO $$
        DECLARE
            _batch_size INT := 10000;
        BEGIN
            
            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'dbo' AND table_name = 'entitylookup') THEN
            
                 SET LOCAL app.changed_by = '3296007F-F9EA-4BD0-B6A6-C8462D54633A';
                 SET LOCAL app.changed_by_system = '3296007F-F9EA-4BD0-B6A6-C8462D54633A';
                 SET LOCAL app.change_operation_id = '3296007F-F9EA-4BD0-B6A6-C8462D54633A';

                LOOP
                    UPDATE dbo.entitylookup
                    SET IsProtected = true
                    WHERE id IN (
                        SELECT id
                        FROM dbo.entitylookup
                        WHERE Key = 'PersonIdentifier' AND IsProtected = false
                        LIMIT _batch_size
                    );

                    EXIT WHEN NOT FOUND;
                END LOOP;
            END IF;
        END
        $$;
        """;
    }
}
