using System.Text;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class ConnectionPackageDefinition : BaseDbDefinition<ConnectionPackage>, IDbDefinition
{
    /// <inheritdoc/>
    public ConnectionPackageDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<ConnectionPackage>(def =>
        {
            def.SetVersion(3);
            def.IsView();

            def.RegisterProperty(t => t.ConnectionId);
            def.RegisterProperty(t => t.PackageId);

            def.RegisterAsCrossReferenceExtended<ExtConnectionPackage, Connection, Package>(
                defineA: (t => t.ConnectionId, t => t.Id, t => t.Connection, false),
                defineB: (t => t.PackageId, t => t.Id, t => t.Package, false)
            );

            var sb = new StringBuilder();
            
            sb.AppendLine($"SELECT ap.{nameof(AssignmentPackage.AssignmentId)} AS {nameof(ConnectionPackage.ConnectionId)}, ap.{nameof(AssignmentPackage.PackageId)} AS {nameof(ConnectionPackage.PackageId)}");
            sb.AppendLine("FROM dbo.assignmentpackage AS ap");

            sb.AppendLine("UNION ALL");
            sb.AppendLine($"SELECT a.{nameof(Assignment.Id)} AS {nameof(ConnectionPackage.ConnectionId)}, rp.{nameof(RolePackage.PackageId)} AS {nameof(ConnectionPackage.PackageId)}");
            sb.AppendLine("FROM dbo.rolepackage AS rp");
            sb.AppendLine("INNER JOIN dbo.assignment AS a ON rp.roleid = a.roleid");
            sb.AppendLine("WHERE rp.entityvariantid IS NULL");

            sb.AppendLine("UNION ALL");
            sb.AppendLine($"SELECT a.{nameof(Assignment.Id)} AS {nameof(ConnectionPackage.ConnectionId)}, rp.{nameof(RolePackage.PackageId)} AS {nameof(ConnectionPackage.PackageId)}");
            sb.AppendLine("FROM dbo.rolepackage AS rp");
            sb.AppendLine("INNER JOIN dbo.assignment AS a ON rp.roleid = a.roleid");
            sb.AppendLine("INNER JOIN dbo.entity AS fe ON a.fromid = fe.id and fe.variantid = rp.entityvariantid");
            sb.AppendLine("WHERE rp.entityvariantid IS NOT NULL");

            sb.AppendLine("UNION ALL");
            sb.AppendLine($"SELECT dp.{nameof(DelegationPackage.DelegationId)} AS {nameof(ConnectionPackage.ConnectionId)}, dp.{nameof(DelegationPackage.PackageId)} AS {nameof(ConnectionPackage.PackageId)}");
            sb.AppendLine("FROM dbo.delegationpackage AS dp");

            def.SetViewQuery(sb.ToString());

            def.AddViewDependency<Connection>();
            def.AddViewDependency<Package>();
            def.AddViewDependency<Assignment>();
            def.AddViewDependency<RolePackage>();
            def.AddViewDependency<DelegationPackage>();
        });
    }
}
