using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class AssignmentDefinition : BaseDbDefinition<Assignment>, IDbDefinition
{
    /// <inheritdoc/>
    public AssignmentDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Assignment>(def =>
        {
            def.EnableAudit();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.FromId);
            def.RegisterProperty(t => t.ToId);
            def.RegisterProperty(t => t.RoleId);

            def.RegisterExtendedProperty<ExtAssignment, Entity>(t => t.FromId, t => t.Id, t => t.From, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtAssignment, Entity>(t => t.ToId, t => t.Id, t => t.To, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtAssignment, Role>(t => t.RoleId, t => t.Id, t => t.Role, cascadeDelete: true);

            def.RegisterUniqueConstraint([t => t.FromId, t => t.ToId, t => t.RoleId], includedProperties: [t => t.Id]);
        });
    }
}
