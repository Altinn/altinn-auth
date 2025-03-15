using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

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
            def.EnableHistory();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.RefId, nullable: true);
            def.RegisterProperty(t => t.LogoUrl, nullable: true);

            def.RegisterUniqueConstraint([t => t.Name]);
        });
    }
}
