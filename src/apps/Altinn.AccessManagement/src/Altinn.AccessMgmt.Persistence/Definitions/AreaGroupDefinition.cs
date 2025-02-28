using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class AreaGroupDefinition : BaseDbDefinition<AreaGroup>, IDbDefinition
{
    /// <inheritdoc/>
    public AreaGroupDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<AreaGroup>(def =>
        {
            def.EnableHistory();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.Description);
            def.RegisterProperty(t => t.EntityTypeId, nullable: true);
            def.RegisterProperty(t => t.Urn, nullable: true);

            def.RegisterExtendedProperty<ExtAreaGroup, EntityType>(t => t.EntityTypeId, t => t.Id, t => t.EntityType, cascadeDelete: true);
            def.RegisterUniqueConstraint([t => t.Name]);
        });
    }
}
