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
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.DelegationId);
            def.RegisterProperty(t => t.PackageId);
            def.RegisterProperty(t => t.AssignmentPackageId, nullable: true);
            def.RegisterProperty(t => t.RolePackageId, nullable: true);

            def.RegisterExtendedProperty<ExtDelegationPackage, Delegation>(t => t.DelegationId, t => t.Id, t => t.Delegation, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtDelegationPackage, Package>(t => t.PackageId, t => t.Id, t => t.Package, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtDelegationPackage, AssignmentPackage>(t => t.AssignmentPackageId, t => t.Id, t => t.AssignmentPackage, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtDelegationPackage, RolePackage>(t => t.RolePackageId, t => t.Id, t => t.RolePackage, cascadeDelete: true);

            def.RegisterUniqueConstraint([t => t.DelegationId, t => t.PackageId]);

            def.AddManualPreMigrationScript(1, PreMigrationScript_RemoveTranslations());
        });
    }

    private string PreMigrationScript_RemoveTranslations()
    {
        return """
            DROP VIEW IF EXISTS translation_history.DelegationPackage;

            DROP TABLE IF EXISTS translation_history._DelegationPackage;
            DROP TABLE IF EXISTS translation.DelegationPackage;

            drop function translation.audit_delegationpackage_delete_fn();
            drop function translation.audit_delegationpackage_update_fn();
            """;
    }
}
