using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class DelegationResourceDefinition : BaseDbDefinition<DelegationResource>, IDbDefinition
{
    /// <inheritdoc/>
    public DelegationResourceDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<DelegationResource>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.DelegationId);
            def.RegisterProperty(t => t.ResourceId);

            def.RegisterExtendedProperty<ExtDelegationResource, Delegation>(t => t.DelegationId, t => t.Id, t => t.Delegation, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtDelegationResource, Resource>(t => t.ResourceId, t => t.Id, t => t.Resource, cascadeDelete: true);

            def.RegisterUniqueConstraint([t => t.DelegationId, t => t.ResourceId]);
        });
    }
}
