using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class AreaDefinition : BaseDbDefinition<Area>, IDbDefinition
{
    /// <inheritdoc/>
    public AreaDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Area>(def =>
        {
            def.SetVersion(2);

            def.EnableAudit();
            def.EnableTranslation();

            def.RegisterPrimaryKey([t => t.Id]);
            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.Description);
            def.RegisterProperty(t => t.IconUrl, nullable: true);
            def.RegisterProperty(t => t.GroupId);
            def.RegisterProperty(t => t.Urn);

            def.RegisterExtendedProperty<ExtArea, AreaGroup>(t => t.GroupId, t => t.Id, t => t.Group, cascadeDelete: true);

            def.RegisterUniqueConstraint([t => t.Name]);
        });
    }
}
