using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RoleLookupDefinition : BaseDbDefinition<RoleLookup>, IDbDefinition
{
    /// <inheritdoc/>
    public RoleLookupDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<RoleLookup>(def =>
        {
            def.EnableHistory();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.RoleId);
            def.RegisterProperty(t => t.Key);
            def.RegisterProperty(t => t.Value);

            def.RegisterExtendedProperty<ExtRoleLookup, Role>(t => t.RoleId, t => t.Id, t => t.Role, cascadeDelete: true);
            def.RegisterUniqueConstraint([t => t.RoleId, t => t.Key]);
        });
    }
}
