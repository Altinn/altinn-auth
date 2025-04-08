using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RoleDefinition : BaseDbDefinition<Role>, IDbDefinition
{
    /// <inheritdoc/>
    public RoleDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Role>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.Code);
            def.RegisterProperty(t => t.Urn);
            def.RegisterProperty(t => t.Description);
            def.RegisterProperty(t => t.IsKeyRole, defaultValue: "false");
            def.RegisterProperty(t => t.ProviderId);
            def.RegisterProperty(t => t.EntityTypeId);

            def.RegisterExtendedProperty<ExtRole, Provider>(t => t.ProviderId, t => t.Id, t => t.Provider, cascadeDelete: false);
            def.RegisterExtendedProperty<ExtRole, EntityType>(t => t.EntityTypeId, t => t.Id, t => t.EntityType, cascadeDelete: false);

            def.RegisterUniqueConstraint([t => t.Urn]);
            def.RegisterUniqueConstraint([t => t.EntityTypeId, t => t.Name]);
            def.RegisterUniqueConstraint([t => t.EntityTypeId, t => t.Code]);
        });
    }
}
