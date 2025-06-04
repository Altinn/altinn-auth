using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class CompactRelationDefinition : BaseDbDefinition<CompactRelation>, IDbDefinition
{
    /// <inheritdoc/>
    public CompactRelationDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<CompactRelation>(def =>
        {
            def.SetVersion(1);
            def.SetType(DbDefinitionType.View);

            def.RegisterExtProperty<ExtCompactRelation>(t => t.FromId, t => t.From, functionName: "CompactEntity");
            def.RegisterExtProperty<ExtCompactRelation>(t => t.RoleId, t => t.Role, functionName: "CompactRole");
            def.RegisterExtProperty<ExtCompactRelation>(t => t.ViaId, t => t.Via, functionName: "CompactEntity", nullable: true);
            def.RegisterExtProperty<ExtCompactRelation>(t => t.ViaRoleId, t => t.ViaRole, functionName: "CompactRole", nullable: true);
            def.RegisterExtProperty<ExtCompactRelation>(t => t.ToId, t => t.To, functionName: "CompactEntity");
            def.RegisterProperty(t => t.Reason);

            def.SetQuery(BasicScript());

            def.AddManualDependency<Entity>();
            def.AddManualDependency<Role>();
            def.AddManualDependency<RoleMap>();
            def.AddManualDependency<Assignment>();
            def.AddManualDependency<Delegation>();
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
        'Direct'::text AS reason
        FROM dbo.assignment a

        UNION ALL

        SELECT a.fromid,
        a.roleid,
        a.toid          AS viaid,
        a2.roleid       AS viaroleid,
        a2.toid,
        'KeyRole'::text AS reason
        FROM dbo.assignment a
        JOIN dbo.assignment a2 ON a.toid = a2.fromid
        JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true

        UNION ALL

        SELECT fa.fromid,
        fa.roleid,
        fa.toid            AS viaid,
        ta.roleid          AS viaroleid,
        ta.toid,
        'Delegation'::text AS reason
        FROM dbo.delegation d
        JOIN dbo.assignment fa ON fa.id = d.fromid
        JOIN dbo.assignment ta ON ta.id = d.toid
        """;
    }
}
