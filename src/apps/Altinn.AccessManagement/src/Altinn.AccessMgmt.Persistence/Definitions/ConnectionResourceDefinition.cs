using System.Text;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class ConnectionResourceDefinition : BaseDbDefinition<ConnectionResource>, IDbDefinition
{
    /// <inheritdoc/>
    public ConnectionResourceDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<ConnectionResource>(def =>
        {
            def.SetVersion(2);
            def.IsView();

            def.RegisterProperty(t => t.ConnectionId);
            def.RegisterProperty(t => t.ResourceId);

            def.RegisterAsCrossReferenceExtended<ExtConnectionResource, Connection, Resource>(
                defineA: (t => t.ConnectionId, t => t.Id, t => t.Connection, false),
                defineB: (t => t.ResourceId, t => t.Id, t => t.Resource, false)
            );

            var sb = new StringBuilder();

            sb.AppendLine($"SELECT ap.{nameof(AssignmentResource.AssignmentId)} AS {nameof(ConnectionResource.ConnectionId)}, ap.{nameof(AssignmentResource.ResourceId)} AS {nameof(ConnectionResource.ResourceId)}");
            sb.AppendLine("FROM dbo.assignmentResource AS ap");
            sb.AppendLine("UNION ALL");
            sb.AppendLine($"SELECT dp.{nameof(DelegationResource.DelegationId)} AS {nameof(ConnectionResource.ConnectionId)}, dp.{nameof(DelegationResource.ResourceId)} AS {nameof(ConnectionResource.ResourceId)}");
            sb.AppendLine("FROM dbo.delegationResource AS dp");

            def.SetQuery(sb.ToString());

            def.AddManualDependency<Connection>();
            def.AddManualDependency<Resource>();
        });
    }
}
