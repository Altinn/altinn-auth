using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class DelegationDefinition : BaseDbDefinition<Delegation>, IDbDefinition
{
    /// <inheritdoc/>
    public DelegationDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Delegation>(def =>
        {
            def.EnableHistory();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.FromId);
            def.RegisterProperty(t => t.ToId);

            def.RegisterExtendedProperty<ExtDelegation, Assignment>(t => t.FromId, t => t.Id, t => t.From, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtDelegation, Assignment>(t => t.ToId, t => t.Id, t => t.To, cascadeDelete: true);

            def.RegisterUniqueConstraint([t => t.FromId, t => t.ToId]);
        });
    }
}
