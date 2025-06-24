using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class RelationDefinition : BaseDbDefinition<Relation>, IDbDefinition
{
    /// <inheritdoc/>
    public RelationDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<Relation>(def =>
        {
            def.SetVersion(1);
            def.SetType(DbDefinitionType.View);

            def.RegisterExtProperty<ExtRelation>(t => t.FromId, t => t.From, functionName: "CompactEntity");
            def.RegisterExtProperty<ExtRelation>(t => t.RoleId, t => t.Role, functionName: "CompactRole");
            def.RegisterExtProperty<ExtRelation>(t => t.ViaId, t => t.Via, functionName: "CompactEntity", nullable: true);
            def.RegisterExtProperty<ExtRelation>(t => t.ViaRoleId, t => t.ViaRole, functionName: "CompactRole", nullable: true);
            def.RegisterExtProperty<ExtRelation>(t => t.ToId, t => t.To, functionName: "CompactEntity");
            def.RegisterExtProperty<ExtRelation>(t => t.PackageId, t => t.Package, functionName: "CompactPackage", nullable: true);
            def.RegisterExtProperty<ExtRelation>(t => t.ResourceId, t => t.Resource, functionName: "CompactResource", nullable: true);
            def.RegisterProperty(t => t.Reason);

            def.SetQuery(BasicScript());

            def.AddManualDependency<Entity>();
            def.AddManualDependency<Role>();
            def.AddManualDependency<RoleMap>();
            def.AddManualDependency<Assignment>();
            def.AddManualDependency<Delegation>();
            def.AddManualDependency<AssignmentPackage>();
            def.AddManualDependency<AssignmentResource>();
            def.AddManualDependency<DelegationPackage>();
            def.AddManualDependency<DelegationResource>();
            def.AddManualDependency<Package>();
            def.AddManualDependency<Resource>();
        });
    }

    private string BasicScript()
    {
        return $"""
        SELECT a.fromid,
        a.roleid,
        NULL::uuid     AS viaid,
        NULL::uuid     AS viaroleid,
        a.toid,
        ap.packageid,
        ar.resourceid,
        'Direct'::text AS reason
        FROM dbo.assignment a
        LEFT OUTER JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        LEFT OUTER JOIN dbo.assignmentresource ar ON ar.assignmentid = a.id

        UNION ALL

        SELECT a.fromid,
        a.roleid,
        NULL::uuid     AS viaid,
        NULL::uuid     AS viaroleid,
        a.toid,
        rp.packageid,
        rr.resourceid,
        'Direct'::text AS reason
        FROM dbo.assignment a
        LEFT JOIN dbo.rolepackage rp ON rp.roleid = a.roleid
        LEFT JOIN dbo.roleresource rr ON rr.roleid = a.roleid

        UNION ALL

        SELECT a.fromid,
        a.roleid,
        a.toid          AS viaid,
        a2.roleid       AS viaroleid,
        a2.toid,
        ap.packageid,
        ar.resourceid,
        'KeyRole'::text AS reason
        FROM dbo.assignment a
        JOIN dbo.assignment a2 ON a.toid = a2.fromid
        JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
        LEFT OUTER JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        LEFT OUTER JOIN dbo.assignmentresource ar ON ar.assignmentid = a.id

        UNION ALL

        SELECT fa.fromid,
        fa.roleid,
        fa.toid            AS viaid,
        ta.roleid          AS viaroleid,
        ta.toid,
        dp.packageid,
        dr.resourceid,
        'Delegation'::text AS reason
        FROM dbo.delegation d
        JOIN dbo.assignment fa ON fa.id = d.fromid
        JOIN dbo.assignment ta ON ta.id = d.toid
        LEFT OUTER JOIN dbo.delegationpackage dp ON dp.delegationid = d.id
        LEFT OUTER JOIN dbo.delegationresource dr ON dr.delegationid = d.id;
        """;
    }
}
