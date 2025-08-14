using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class ProviderDefinition : BaseDbDefinition<Provider>, IDbDefinition
{
    /// <inheritdoc/>
    public ProviderDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Provider>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();

            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.RefId, nullable: true);
            def.RegisterProperty(t => t.LogoUrl, nullable: true);
            def.RegisterProperty(t => t.Code, nullable: true);
            def.RegisterProperty(t => t.TypeId, nullable: true);

            def.RegisterExtendedProperty<ExtProvider, ProviderType>(t => t.TypeId, t => t.Id, t => t.Type);

            def.RegisterUniqueConstraint([t => t.Name]);
        });
    }
}
