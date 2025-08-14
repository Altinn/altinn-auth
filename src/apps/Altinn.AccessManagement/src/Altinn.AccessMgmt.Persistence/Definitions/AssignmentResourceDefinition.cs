using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class AssignmentResourceDefinition : BaseDbDefinition<AssignmentResource>, IDbDefinition
{
    /// <inheritdoc/>
    public AssignmentResourceDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<AssignmentResource>(def =>
        {
            def.EnableAudit();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.AssignmentId);
            def.RegisterProperty(t => t.ResourceId);

            def.RegisterAsCrossReferenceExtended<ExtAssignmentResource, Assignment, Resource>(
                defineA: (t => t.AssignmentId, t => t.Id, t => t.Assignment, CascadeDelete: true),
                defineB: (t => t.ResourceId, t => t.Id, t => t.Resource, CascadeDelete: true)
            );

            def.RegisterUniqueConstraint([t => t.AssignmentId, t => t.ResourceId]);
        });
    }
}
