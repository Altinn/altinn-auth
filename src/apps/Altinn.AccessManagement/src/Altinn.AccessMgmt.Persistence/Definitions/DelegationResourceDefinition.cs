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
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.DelegationId);
            def.RegisterProperty(t => t.ResourceId);

            def.RegisterExtendedProperty<ExtDelegationResource, Delegation>(t => t.DelegationId, t => t.Id, t => t.Delegation, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtDelegationResource, Resource>(t => t.ResourceId, t => t.Id, t => t.Resource, cascadeDelete: true);

            def.RegisterUniqueConstraint([t => t.DelegationId, t => t.ResourceId]);

            def.AddManualPreMigrationScript(1, PreMigrationScript_RemoveTranslations());
        });
    }

    private string PreMigrationScript_RemoveTranslations()
    {
        return """
            DROP VIEW IF EXISTS translation_history.DelegationResource;

            DROP TABLE IF EXISTS translation_history._DelegationResource;
            DROP TABLE IF EXISTS translation.DelegationResource;

            drop function translation.audit_delegationresource_delete_fn();
            drop function translation.audit_delegationresource_update_fn();
            """;
    }
}
