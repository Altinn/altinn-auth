using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class EntityVariantRoleDefinition : BaseDbDefinition<EntityVariantRole>, IDbDefinition
{
    /// <inheritdoc/>
    public EntityVariantRoleDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<EntityVariantRole>(def =>
        {
            def.EnableAudit();
            def.EnableTranslation();
            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.VariantId);
            def.RegisterProperty(t => t.RoleId);

            def.RegisterAsCrossReferenceExtended<ExtEntityVariantRole, EntityVariant, Role>(
                defineA: (t => t.VariantId, t => t.Id, t => t.Variant, CascadeDelete: false),
                defineB: (t => t.RoleId, t => t.Id, t => t.Role, CascadeDelete: true)
            );

            def.RegisterUniqueConstraint([t => t.VariantId, t => t.RoleId]);
        });
    }
}
