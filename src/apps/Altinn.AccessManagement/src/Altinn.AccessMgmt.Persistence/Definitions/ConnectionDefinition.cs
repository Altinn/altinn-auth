using System.Text;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class ConnectionDefinition : BaseDbDefinition<Connection>, IDbDefinition
{
    /// <inheritdoc/>
    public ConnectionDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Connection>(def =>
        {
            def.SetVersion(2);
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

            def.RegisterExtendedProperty<ExtConnection, Entity>(t => t.FromId, t => t.Id, t => t.From);
            def.RegisterExtendedProperty<ExtConnection, Role>(t => t.RoleId, t => t.Id, t => t.Role);
            def.RegisterExtendedProperty<ExtConnection, Entity>(t => t.ToId, t => t.Id, t => t.To);
            def.RegisterExtendedProperty<ExtConnection, Entity>(t => t.FacilitatorId, t => t.Id, t => t.Facilitator, optional: true);
            def.RegisterExtendedProperty<ExtConnection, Role>(t => t.FacilitatorRoleId, t => t.Id, t => t.FacilitatorRole, optional: true);

            var basicScript = BasicScript();
            var extendedScript = ExtendedScript();

            def.SetQuery(basicScript, extendedScript);

            def.AddManualDependency<Assignment>();
            def.AddManualDependency<Delegation>();
            def.AddManualDependency<Role>();
            def.AddManualDependency<RoleMap>();
            def.AddManualDependency<Entity>();
        });
    }

    private string BasicScript()
    {
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

        sb.AppendLine("SELECT a.*");
        sb.AppendLine("FROM a3 a");
        sb.AppendLine("WHERE a.fromid = COALESCE(@fromid::uuid, a.fromid)");
        sb.AppendLine("AND a.toid = COALESCE(@toid::uuid, a.toid);");

        return sb.ToString();
    }

    private string ExtendedScript()
    {
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

        sb.AppendLine("SELECT a.*, ");

        /*EXTENDED*/
        sb.AppendLine(EntityColumns("fe", "From") + ",");
        sb.AppendLine(EntityColumns("te", "To") + ",");
        sb.AppendLine(RoleColumns("r", "Role") + ",");
        sb.AppendLine(EntityColumns("ve", "Facilitator") + ",");
        sb.AppendLine(RoleColumns("vr", "FacilitatorRole") + " ");
        //// sb.AppendLine(DelegationColumns("d", "Delegation") + " ");

        sb.AppendLine("FROM a3 a");

        /*EXTENDED*/
        sb.AppendLine("JOIN dbo.entity fe ON a.fromid = fe.id");
        sb.AppendLine("JOIN dbo.entity te ON a.toid   = te.id");
        sb.AppendLine("JOIN dbo.role r ON a.roleid = r.id");
        sb.AppendLine("LEFT JOIN dbo.entity ve ON a.viaid = ve.id");
        sb.AppendLine("LEFT JOIN dbo.role vr ON a.viaroleid = vr.id");
        
        sb.AppendLine("WHERE a.fromid = COALESCE(@fromid::uuid, a.fromid)");
        sb.AppendLine("AND a.toid = COALESCE(@toid::uuid, a.toid);");

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

    private string DelegationColumns(string alias, string prefix)
    {
        return
            $"{alias}.{nameof(Delegation.Id)} as {prefix}_{nameof(Delegation.Id)}, " +
            $"{alias}.{nameof(Delegation.FromId)} as {prefix}_{nameof(Delegation.FromId)}, " +
            $"{alias}.{nameof(Delegation.ToId)} as {prefix}_{nameof(Delegation.ToId)}, " +
            $"{alias}.{nameof(Delegation.FacilitatorId)} as {prefix}_{nameof(Delegation.FacilitatorId)}";
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
}
