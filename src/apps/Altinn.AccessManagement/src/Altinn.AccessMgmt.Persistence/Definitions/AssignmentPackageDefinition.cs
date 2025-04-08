using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class AssignmentPackageDefinition : BaseDbDefinition<AssignmentPackage>, IDbDefinition
{
    /// <inheritdoc/>
    public AssignmentPackageDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<AssignmentPackage>(def =>
        {
            def.EnableAudit();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.AssignmentId);
            def.RegisterProperty(t => t.PackageId);

            def.RegisterAsCrossReferenceExtended<ExtAssignmentPackage, Assignment, Package>(
                defineA: (t => t.AssignmentId, t => t.Id, t => t.Assignment, CascadeDelete: true),
                defineB: (t => t.PackageId, t => t.Id, t => t.Package, CascadeDelete: true)
            );

            def.RegisterUniqueConstraint([t => t.AssignmentId, t => t.PackageId]);
        });
    }
}
