using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class ResourceDefinition : BaseDbDefinition<Resource>, IDbDefinition
{
    /// <inheritdoc/>
    public ResourceDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Resource>(def =>
        {
            def.EnableHistory();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.Description);
            def.RegisterProperty(t => t.RefId);
            def.RegisterProperty(t => t.ProviderId);
            def.RegisterProperty(t => t.TypeId);

            def.RegisterExtendedProperty<ExtResource, Provider>(t => t.ProviderId, t => t.Id, t => t.Provider, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtResource, ResourceType>(t => t.TypeId, t => t.Id, t => t.Type, cascadeDelete: false);

            def.RegisterUniqueConstraint([t => t.ProviderId, t => t.RefId]);
        });
    }
}
