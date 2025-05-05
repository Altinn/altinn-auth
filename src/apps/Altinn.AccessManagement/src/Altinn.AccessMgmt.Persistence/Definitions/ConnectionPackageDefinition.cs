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

            def.SetQuery(GetScript(extended: false), GetScript(extended: true));

            def.AddManualDependency<Connection>();
            def.AddManualDependency<Package>();
            def.AddManualDependency<Assignment>();
            def.AddManualDependency<RolePackage>();
            def.AddManualDependency<DelegationPackage>();
        });
    }

    private string GetScript(bool extended)
    {
        var sb = new StringBuilder();

        sb.AppendLine("WITH a1 AS (");

        // DIRECT
        sb.AppendLine("SELECT a.id, a.fromid, NULL::uuid AS viaid, NULL::uuid AS viaroleid, a.toid, a.roleid,");
        sb.AppendLine("'DIRECT' AS source, 1 AS isdirect, 0 AS isparent, 0 AS isrolemap, 0 AS iskeyrole");
        sb.AppendLine("FROM dbo.assignment a");
        sb.AppendLine("WHERE a.fromid = COALESCE(@fromid, a.fromid)::uuid");
        sb.AppendLine("AND a.toid   = COALESCE(@toid, a.toid)::uuid");

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
        sb.AppendLine("WHERE fe.id    = COALESCE(@fromid, fe.id)::uuid");
        sb.AppendLine("AND a.toid   = COALESCE(@toid, a.toid)::uuid");

        sb.AppendLine("),");

        sb.AppendLine("a2 AS(");
        sb.AppendLine("SELECT * FROM a1");

        sb.AppendLine("UNION ALL");

        sb.AppendLine("SELECT x.id, x.fromid, x.fromid AS viaid, x.roleid AS viaroleid, x.toid, rm.getroleid AS roleid, ");
        sb.AppendLine("x.source || 'MAP' AS source, x.isdirect, x.isparent, 1 AS isrolemap, x.iskeyrole");
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

        if (extended)
        {
            sb.AppendLine("SELECT result.*, row_number() over (order by result.id) as _rownum, ");
            sb.AppendLine(PackageColumns("pck", "Package") + ",");
            sb.AppendLine(EntityColumns("fe", "From") + ",");
            sb.AppendLine(EntityColumns("te", "To") + ",");
            sb.AppendLine(RoleColumns("r", "Role") + ",");
            sb.AppendLine(EntityColumns("ve", "Facilitator") + ",");
            sb.AppendLine(RoleColumns("vr", "FacilitatorRole") + " ");
        }
        else
        {
            sb.AppendLine("SELECT result.*, row_number() over (order by result.id) as _rownum ");
        }

        sb.AppendLine("FROM result");

        if (extended)
        {
            sb.AppendLine("JOIN dbo.package pck ON result.packageid = pck.id");
            sb.AppendLine("JOIN dbo.entity fe ON result.fromid = fe.id");
            sb.AppendLine("JOIN dbo.entity te ON result.toid   = te.id");
            sb.AppendLine("JOIN dbo.role r ON result.roleid = r.id");
            sb.AppendLine("LEFT JOIN dbo.entity ve ON result.viaid = ve.id");
            sb.AppendLine("LEFT JOIN dbo.role vr ON result.viaroleid = vr.id");
        }

        sb.AppendLine("WHERE result.fromid = COALESCE(@fromid, result.fromid)::uuid");
        sb.AppendLine("AND result.toid = COALESCE(@toid, result.toid)::uuid;");

        return sb.ToString();
    }

    private string EntityColumns(string alias, string prefix)
    {
        return
            $"{alias}.{nameof(Entity.Id)} as {prefix}_{nameof(Entity.Id)}, " +
            $"{alias}.{nameof(Entity.TypeId)} as {prefix}_{nameof(Entity.TypeId)}, " +
            $"{alias}.{nameof(Entity.VariantId)} as {prefix}_{nameof(Entity.VariantId)}, " +
            $"{alias}.{nameof(Entity.Name)} as {prefix}_{nameof(Entity.Name)}, " +
            $"{alias}.{nameof(Entity.RefId)} as {prefix}_{nameof(Entity.RefId)}, " +
            $"{alias}.{nameof(Entity.ParentId)} as {prefix}_{nameof(Entity.ParentId)}";
    }

    private string RoleColumns(string alias, string prefix)
    {
        return
            $"{alias}.{nameof(Role.Id)} as {prefix}_{nameof(Role.Id)}, " +
            $"{alias}.{nameof(Role.EntityTypeId)} as {prefix}_{nameof(Role.EntityTypeId)}, " +
            $"{alias}.{nameof(Role.ProviderId)} as {prefix}_{nameof(Role.ProviderId)}, " +
            $"{alias}.{nameof(Role.Name)} as {prefix}_{nameof(Role.Name)}, " +
            $"{alias}.{nameof(Role.Code)} as {prefix}_{nameof(Role.Code)}, " +
            $"{alias}.{nameof(Role.Description)} as {prefix}_{nameof(Role.Description)}, " +
            $"{alias}.{nameof(Role.IsKeyRole)} as {prefix}_{nameof(Role.IsKeyRole)}, " +
            $"{alias}.{nameof(Role.Urn)} as {prefix}_{nameof(Role.Urn)}";
    }

    private string PackageColumns(string alias, string prefix)
    {
        return
            $"{alias}.{nameof(Package.Id)} as {prefix}_{nameof(Package.Id)}, " +
            $"{alias}.{nameof(Package.ProviderId)} as {prefix}_{nameof(Package.ProviderId)}, " +
            $"{alias}.{nameof(Package.EntityTypeId)} as {prefix}_{nameof(Package.EntityTypeId)}, " +
            $"{alias}.{nameof(Package.AreaId)} as {prefix}_{nameof(Package.AreaId)}, " +
            $"{alias}.{nameof(Package.Name)} as {prefix}_{nameof(Package.Name)}, " +
            $"{alias}.{nameof(Package.Description)} as {prefix}_{nameof(Package.Description)}, " +
            $"{alias}.{nameof(Package.IsAssignable)} as {prefix}_{nameof(Package.IsAssignable)}, " +
            $"{alias}.{nameof(Package.IsDelegable)} as {prefix}_{nameof(Package.IsDelegable)}, " +
            $"{alias}.{nameof(Package.HasResources)} as {prefix}_{nameof(Package.HasResources)}, " +
            $"{alias}.{nameof(Package.Urn)} as {prefix}_{nameof(Package.Urn)}";
    }
}
