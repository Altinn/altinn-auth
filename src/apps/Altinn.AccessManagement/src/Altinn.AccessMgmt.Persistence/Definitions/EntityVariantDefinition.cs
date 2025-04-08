using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class EntityVariantDefinition : BaseDbDefinition<EntityVariant>, IDbDefinition
{
    /// <inheritdoc/>
    public EntityVariantDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<EntityVariant>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.Description);
            def.RegisterProperty(t => t.TypeId);

            def.RegisterExtendedProperty<ExtEntityVariant, EntityType>(t => t.TypeId, t => t.Id, t => t.Type, cascadeDelete: true);
            def.RegisterUniqueConstraint([t => t.TypeId, t => t.Name]);
        });
    }
}
