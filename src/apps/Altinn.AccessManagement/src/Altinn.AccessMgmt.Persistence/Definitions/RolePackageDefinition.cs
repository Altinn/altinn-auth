using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RolePackageDefinition : BaseDbDefinition<RolePackage>, IDbDefinition
{
    /// <inheritdoc/>
    public RolePackageDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<RolePackage>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.RoleId);
            def.RegisterProperty(t => t.PackageId);
            def.RegisterProperty(t => t.HasAccess);
            def.RegisterProperty(t => t.CanDelegate);
            def.RegisterProperty(t => t.EntityVariantId!, nullable: true);

            def.RegisterExtendedProperty<ExtRolePackage, Role>(t => t.RoleId, t => t.Id, t => t.Role, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtRolePackage, Package>(t => t.PackageId, t => t.Id, t => t.Package, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtRolePackage, EntityVariant>(t => t.EntityVariantId!, t => t.Id, t => t.EntityVariant!, cascadeDelete: false, optional: true);

            def.RegisterUniqueConstraint([t => t.RoleId, t => t.PackageId, t => t.EntityVariantId!]);
        });
    }
}
