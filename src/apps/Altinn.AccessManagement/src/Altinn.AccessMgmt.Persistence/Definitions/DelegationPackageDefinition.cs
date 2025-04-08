using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class DelegationPackageDefinition : BaseDbDefinition<DelegationPackage>, IDbDefinition
{
    /// <inheritdoc/>
    public DelegationPackageDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<DelegationPackage>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.DelegationId);
            def.RegisterProperty(t => t.PackageId);

            def.RegisterExtendedProperty<ExtDelegationPackage, Delegation>(t => t.DelegationId, t => t.Id, t => t.Delegation, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtDelegationPackage, Package>(t => t.PackageId, t => t.Id, t => t.Package, cascadeDelete: true);
            
            def.RegisterUniqueConstraint([t => t.DelegationId, t => t.PackageId]);
        });
    }
}
