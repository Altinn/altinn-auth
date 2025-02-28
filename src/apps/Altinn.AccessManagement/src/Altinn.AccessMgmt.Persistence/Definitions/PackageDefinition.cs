using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class PackageDefinition : BaseDbDefinition<Package>, IDbDefinition
{
    /// <inheritdoc/>
    public PackageDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Package>(def =>
        {
            def.EnableHistory();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.Description);
            def.RegisterProperty(t => t.Urn);
            def.RegisterProperty(t => t.IsDelegable);
            def.RegisterProperty(t => t.HasResources);
            def.RegisterProperty(t => t.ProviderId);
            def.RegisterProperty(t => t.EntityTypeId);
            def.RegisterProperty(t => t.AreaId);

            def.RegisterExtendedProperty<ExtPackage, Provider>(t => t.ProviderId, t => t.Id, t => t.Provider, cascadeDelete: false);
            def.RegisterExtendedProperty<ExtPackage, EntityType>(t => t.EntityTypeId, t => t.Id, t => t.EntityType, cascadeDelete: false);
            def.RegisterExtendedProperty<ExtPackage, Area>(t => t.AreaId, t => t.Id, t => t.Area, cascadeDelete: false);

            def.RegisterUniqueConstraint([t => t.ProviderId, t => t.Name]);
        });
    }
}
