using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RoleResourceDefinition : BaseDbDefinition<RoleResource>, IDbDefinition
{
    /// <inheritdoc/>
    public RoleResourceDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<RoleResource>(def =>
        {
            def.EnableHistory();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.RoleId);
            def.RegisterProperty(t => t.ResourceId);

            def.RegisterAsCrossReferenceExtended<ExtRoleResource, Role, Resource>(
               defineA: (t => t.RoleId, t => t.Id, t => t.Role, CascadeDelete: true),
               defineB: (t => t.ResourceId, t => t.Id, t => t.Resource, CascadeDelete: true)
            );

            def.RegisterUniqueConstraint([t => t.RoleId, t => t.ResourceId]);
        });
    }
}
