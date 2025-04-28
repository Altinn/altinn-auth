using System.Text;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class EntityPartyDefinition : BaseDbDefinition<EntityParty>, IDbDefinition
{
    /// <inheritdoc/>
    public EntityPartyDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<EntityParty>(def =>
        {
            def.SetVersion(2);
            def.IsView();

            def.RegisterProperty(t => t.Id);
            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.RefId);
            def.RegisterProperty(t => t.Type);
            def.RegisterProperty(t => t.Variant);

            var sb = new StringBuilder();

            sb.AppendLine($"SELECT e.{nameof(Entity.Id)} AS {nameof(EntityParty.Id)}, e.{nameof(Entity.Name)} AS {nameof(EntityParty.Name)}, e.{nameof(Entity.RefId)} AS {nameof(EntityParty.RefId)}, et.{nameof(EntityType.Name)} AS {nameof(EntityParty.Type)}, ev.{nameof(EntityVariant.Name)} AS {nameof(EntityParty.Variant)}");
            sb.AppendLine($"FROM dbo.entity AS e");
            sb.AppendLine($"INNER JOIN dbo.entitytype AS et ON e.{nameof(Entity.TypeId)} = et.{nameof(EntityType.Id)}");
            sb.AppendLine($"INNER JOIN dbo.entityvariant AS ev ON e.{nameof(Entity.VariantId)} = ev.{nameof(EntityVariant.Id)}");
            
            def.SetQuery(sb.ToString());

            def.AddManualDependency<Entity>();
            def.AddManualDependency<EntityType>();
            def.AddManualDependency<EntityVariant>();
        });
    }
}
