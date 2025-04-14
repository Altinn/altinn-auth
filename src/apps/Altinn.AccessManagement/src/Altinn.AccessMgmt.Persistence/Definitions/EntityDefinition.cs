using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class EntityDefinition : BaseDbDefinition<Entity>, IDbDefinition
{
    /// <inheritdoc/>
    public EntityDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Entity>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.RefId);
            def.RegisterProperty(t => t.TypeId);
            def.RegisterProperty(t => t.VariantId);
            def.RegisterProperty(t => t.ParentId, nullable: true);

            def.RegisterExtendedProperty<ExtEntity, EntityType>(t => t.TypeId, t => t.Id, t => t.Type, cascadeDelete: false);
            def.RegisterExtendedProperty<ExtEntity, EntityVariant>(t => t.VariantId, t => t.Id, t => t.Variant, cascadeDelete: false);
            def.RegisterExtendedProperty<ExtEntity, Entity>(t => t.ParentId, t => t.Id, t => t.Parent, optional: true, cascadeDelete: false);

            //// def.RegisterUniqueConstraint([t => t.Name, t => t.TypeId, t => t.RefId]);
        });
    }
}
