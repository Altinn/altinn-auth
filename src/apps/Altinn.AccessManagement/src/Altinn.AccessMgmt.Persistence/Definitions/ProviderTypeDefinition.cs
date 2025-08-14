using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class ProviderTypeDefinition : BaseDbDefinition<ProviderType>, IDbDefinition
{
    /// <inheritdoc/>
    public ProviderTypeDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<ProviderType>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();

            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);
            def.RegisterProperty(t => t.Name);

            def.RegisterUniqueConstraint([t => t.Name]);
        });
    }
}
