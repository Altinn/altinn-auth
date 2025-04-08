using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class PackageResourceDefinition : BaseDbDefinition<PackageResource>, IDbDefinition
{
    /// <inheritdoc/>
    public PackageResourceDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<PackageResource>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.PackageId);
            def.RegisterProperty(t => t.ResourceId);

            def.RegisterAsCrossReferenceExtended<ExtPackageResource, Package, Resource>(
                defineA: (t => t.PackageId, t => t.Id, t => t.Package, CascadeDelete: true),
                defineB: (t => t.ResourceId, t => t.Id, t => t.Resource, CascadeDelete: true)
            );

            def.RegisterUniqueConstraint([t => t.PackageId, t => t.ResourceId]);
        });
    }
}
