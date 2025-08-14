using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RoleMapDefinition : BaseDbDefinition<RoleMap>, IDbDefinition
{
    /// <inheritdoc/>
    public RoleMapDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<RoleMap>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.HasRoleId);
            def.RegisterProperty(t => t.GetRoleId);

            def.RegisterExtendedProperty<ExtRoleMap, Role>(t => t.HasRoleId, t => t.Id, t => t.HasRole, cascadeDelete: true);
            def.RegisterExtendedProperty<ExtRoleMap, Role>(t => t.GetRoleId, t => t.Id, t => t.GetRole, cascadeDelete: true);

            def.RegisterUniqueConstraint([t => t.HasRoleId, t => t.GetRoleId]);
        });
    }
}
