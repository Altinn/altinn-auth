using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class EntityTypeDefinition : BaseDbDefinition<EntityType>, IDbDefinition
{
    /// <inheritdoc/>
    public EntityTypeDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<EntityType>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.ProviderId);

            def.RegisterExtendedProperty<ExtEntityType, Provider>(t => t.ProviderId, t => t.Id, t => t.Provider, cascadeDelete: true);
            def.RegisterUniqueConstraint([t => t.ProviderId, t => t.Name]);
        });
    }
}
