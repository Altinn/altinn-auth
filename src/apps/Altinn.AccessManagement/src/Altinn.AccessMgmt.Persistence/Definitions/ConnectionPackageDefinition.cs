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
            def.SetType(DbDefinitionType.Query);

            def.RegisterProperty(t => t.Id);

            def.RegisterProperty(t => t.FromId);
            def.RegisterProperty(t => t.RoleId);
            def.RegisterProperty(t => t.ToId);
            def.RegisterProperty(t => t.FacilitatorId, nullable: true);
            def.RegisterProperty(t => t.FacilitatorRoleId, nullable: true);

            def.RegisterProperty(t => t.Source);

            def.RegisterProperty(t => t.IsDirect);
            def.RegisterProperty(t => t.IsParent);
            def.RegisterProperty(t => t.IsRoleMap);
            def.RegisterProperty(t => t.IsKeyRole);

            def.RegisterProperty(t => t.PackageId);
            def.RegisterProperty(t => t.HasAccess);
            def.RegisterProperty(t => t.CanAssign);
            def.RegisterProperty(t => t.PackageSource);

            def.RegisterAsCrossReferenceExtended<ExtConnectionPackage, Connection, Package>(
                defineA: (t => t.Id, t => t.Id, t => t.Connection, false),
                defineB: (t => t.PackageId, t => t.Id, t => t.Package, false)
            );

            var sb = new StringBuilder();

            sb.AppendLine("WITH a1 AS (");

            // DIRECT
            sb.AppendLine("SELECT a.id, a.fromid, NULL::uuid AS viaid, NULL::uuid AS viaroleid, a.toid, a.roleid,");
            sb.AppendLine("'DIRECT' AS source, 1 AS isdirect, 0 AS isparent, 0 AS isrolemap, 0 AS iskeyrole");
            sb.AppendLine("FROM dbo.assignment a");
            sb.AppendLine("WHERE a.fromid = COALESCE(@fromid::uuid, a.fromid)");
            sb.AppendLine("AND a.toid   = COALESCE(@toid::uuid, a.toid)");

            sb.AppendLine("UNION ALL");

            // PARENT
            sb.AppendLine("SELECT");
            sb.AppendLine("a.id,");
            sb.AppendLine("fe.id AS fromid,");
            sb.AppendLine("a.fromid AS viaid,");
            sb.AppendLine("NULL::uuid AS viaroleid,");
            sb.AppendLine("a.toid,");
            sb.AppendLine("a.roleid,");
            sb.AppendLine("'PARENT' AS source,");
            sb.AppendLine("0 AS isdirect,");
            sb.AppendLine("1 AS isparent,");
            sb.AppendLine("0 AS isrolemap,");
            sb.AppendLine("0 AS iskeyrole");
            sb.AppendLine("FROM dbo.assignment a");
            sb.AppendLine("JOIN dbo.entity fe   ON a.fromid = fe.parentid");
            sb.AppendLine("WHERE fe.id    = COALESCE(@fromid::uuid, fe.id)");
            sb.AppendLine("AND a.toid   = COALESCE(@toid::uuid, a.toid)");

            sb.AppendLine("),");

            sb.AppendLine("a2 AS(");
            sb.AppendLine("SELECT * FROM a1");

            sb.AppendLine("UNION ALL");
            
            sb.AppendLine("SELECT x.id, x.fromid, x.fromid AS viaid, x.roleid AS viaroleid, x.toid, rm.getroleid AS roleid, ");
            sb.AppendLine("x.source || 'MAP' AS source, x.isdirect, x.isparent, 1             AS isrolemap, x.iskeyrole");
            sb.AppendLine("FROM a1 x");
            sb.AppendLine("JOIN dbo.rolemap rm ON x.roleid = rm.hasroleid");
            sb.AppendLine("),");

            sb.AppendLine("a3 AS(");
            sb.AppendLine("SELECT* FROM a2");

            sb.AppendLine("UNION ALL");

            sb.AppendLine("SELECT s.id, s.fromid, s.toid AS viaid, s.roleid AS viaroleid, a.toid, a.roleid, ");
            sb.AppendLine("s.source || 'KEY' AS source, s.isdirect, s.isparent, s.isrolemap, 1 AS iskeyrole");
            sb.AppendLine("FROM a2 s");
            sb.AppendLine("JOIN dbo.assignment a ON s.toid = a.fromid");
            sb.AppendLine("JOIN dbo.role r ON a.roleid = r.id");
            sb.AppendLine("AND r.iskeyrole = TRUE");
            sb.AppendLine(")");
            sb.AppendLine(",result AS(");
            sb.AppendLine("SELECT Source.*, AP.PackageId, 1::bool AS HasAccess, 1::bool AS CanAssign, 'DIRECT' AS PackageSource");
            sb.AppendLine("FROM a3 AS Source");
            sb.AppendLine("JOIN dbo.AssignmentPackage AS AP ON AP.AssignmentId = Source.Id");
            sb.AppendLine("UNION ALL");
            sb.AppendLine("SELECT Source.*, AP.PackageId, AP.HasAccess, AP.CanDelegate AS CanAssign, 'ROLE' AS PackageSource");
            sb.AppendLine("FROM a3 AS Source");
            sb.AppendLine("JOIN dbo.RolePackage AS AP ON AP.RoleId = Source.RoleId");
            sb.AppendLine(")");

            sb.AppendLine("SELECT result.*");
            sb.AppendLine("FROM result");

            sb.AppendLine("WHERE result.fromid = COALESCE(@fromid::uuid, result.fromid)");
            sb.AppendLine("AND result.toid = COALESCE(@toid::uuid, result.toid);");


            def.SetQuery(sb.ToString());

            def.AddManualDependency<Connection>();
            def.AddManualDependency<Package>();
            def.AddManualDependency<Assignment>();
            def.AddManualDependency<RolePackage>();
            def.AddManualDependency<DelegationPackage>();
        });
    }
}
