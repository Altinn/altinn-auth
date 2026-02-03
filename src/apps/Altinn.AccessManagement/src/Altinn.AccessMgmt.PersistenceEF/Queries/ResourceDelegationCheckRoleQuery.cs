using System.Data;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Altinn.AccessMgmt.PersistenceEF.Queries;

/// <summary>
/// Custom query extensions for role lookup for resource delegation checks
/// </summary>
public static class ResourceDelegationCheckRoleQuery
{
    public static async Task<IReadOnlyList<RoleDelegationCheck>> GetRolesForResourceDelegationCheck(
        this AppDbContext dbContext,
        Guid fromId,
        Guid toId,
        bool toIsMainAdminForFrom = false,
        CancellationToken ct = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<RoleDelegationCheckRow>(
                QUERY,
                new NpgsqlParameter("fromId", fromId),
                new NpgsqlParameter("toId", toId),
                new NpgsqlParameter("isMainAdmin", NpgsqlDbType.Boolean) { Value = toIsMainAdminForFrom }
            )
            .AsNoTracking()
            .ToListAsync(ct);

        // Project rows back into your nested model
        return rows.Select(r => new RoleDelegationCheck
        {
            Role = new Role
            {
                Id = r.RoleId
            },
            Result = r.HasAccess == true || r.CanDelegate == true,
            Reason = new RoleDelegationCheck.RoleReason
            {
                Description = r.Reason,
                RoleId = r.AssignmentRoleId,
                RoleUrn = r.AssignmentRoleUrn,
                FromId = r.FromId,
                FromName = r.FromName,
                ToId = r.ToId,
                ToName = r.ToName,
                ViaId = r.ViaId,
                ViaName = r.ViaName,
                ViaRoleId = r.ViaRoleId,
                ViaRoleUrn = r.ViaRoleUrn
            }
        }).ToList();
    }

    private static readonly string QUERY = /* sql */ """
        WITH mainUnit AS (  
            -- Get main unit of from-party if exists
            SELECT 
                a.toid AS id,
                a.roleid AS roleid
            FROM dbo.assignment a
                JOIN dbo.role r ON a.roleid = r.id
            WHERE a.fromid = @fromId
                AND r.code IN ('hovedenhet', 'ikke-naeringsdrivende-hovedenhet')
        ),
        allRoles AS (
            -- Get all roles relevant for from-party
            SELECT 
                r.id AS roleid,
                r.urn AS roleurn,
                r.legacyurn AS rolelegacyurn,
                r.isassignable,
                false AS isdelegable -- Roles does not have delegable property, only packages
            FROM dbo.role r
            WHERE r.isavailableforserviceowners = true      -- Unsure if this is sufficient filter for relevant roles
        ),
        mainAdminRoles AS (
            -- Get all roles for main administrator
            SELECT 
                r.id AS roleid,
                r.urn AS roleurn,
                r.legacyurn AS rolelegacyurn,
                r.isassignable,
                false AS isdelegable -- Roles does not have delegable property, only packages
            FROM dbo.role r
                JOIN dbo.rolemap rm ON rm.getroleid = r.id
                JOIN dbo.role ar ON rm.hasroleid = ar.id
            WHERE ar.code = 'hovedadministrator'
                AND @isMainAdmin = true
        ),
        userRoles AS (
            -- Get direct assignment roles to user/party
            SELECT
                a.fromid,
                a.roleid        AS assroleid,
                NULL::uuid      AS viaid, 
                NULL::uuid      AS viaroleid,
                a.toid,
                true            AS hasaccess,
                true            AS candelegate,
                r.roleid,
                r.roleurn,
                r.rolelegacyurn,
                r.isassignable,
                r.isdelegable,
                true AS isassignmentrole,
                false AS isrolemap,
                false AS isthroughkeyrole,
                false AS isthroughmainunit,
                'Direct-AssignmentRole'::text AS reason
            FROM dbo.assignment a
                JOIN allRoles r ON a.roleid = r.roleid
            WHERE a.fromid = @fromId AND a.toid = @toId

            UNION ALL

            -- Get direct assignment roles to user/party from main unit
            SELECT
                a.fromid,
                a.roleid        AS assroleid,
                NULL::uuid      AS viaid, 
                NULL::uuid      AS viaroleid,
                a.toid,
                true            AS hasaccess,
                true            AS candelegate,
                r.roleid,
                r.roleurn,
                r.rolelegacyurn,
                r.isassignable,
                r.isdelegable,
                true AS isassignmentrole,
                false AS isrolemap,
                false AS isthroughkeyrole,
                true AS ismainunitrole,
                'Direct-AssignmentRole-MainUnit'::text AS reason
            FROM dbo.assignment a
                JOIN allRoles r ON a.roleid = r.roleid
            WHERE a.fromid IN (SELECT id FROM mainUnit) AND a.toid = @toId

            UNION ALL

            -- Get assignment roles to user/party's key-roles
            SELECT
                a.fromid,
                a.roleid        AS assroleid,
                a2.fromid       AS viaid, 
                a2.roleid       AS viaroleid,
                a2.toid,
                true            AS hasaccess,
                true            AS candelegate,
                r.roleid,
                r.roleurn,
                r.rolelegacyurn,
                r.isassignable,
                r.isdelegable,
                true AS isassignmentrole,
                false AS isrolemap,
                true AS isthroughkeyrole,
                false AS isthroughmainunit,
                'KeyRole-AssignmentRole'::text AS reason
            FROM dbo.assignment a
                JOIN dbo.assignment a2 ON a.toid = a2.fromid
                JOIN dbo.role keyrole ON a2.roleid = keyrole.id AND keyrole.iskeyrole = true
                JOIN allRoles r ON a.roleid = r.roleid
            WHERE a.fromid = @fromId AND a2.toid = @toId

            UNION ALL

            -- Get assignment roles to user/party's key-roles from main unit
            SELECT
                a.fromid,
                a.roleid        AS assroleid,
                a2.fromid       AS viaid, 
                a2.roleid       AS viaroleid,
                a2.toid,
                true            AS hasaccess,
                true            AS candelegate,
                r.roleid,
                r.roleurn,
                r.rolelegacyurn,
                r.isassignable,
                r.isdelegable,
                true AS isassignmentrole,
                false AS isrolemap,
                true AS isthroughkeyrole,
                false AS isthroughmainunit,
                'KeyRole-AssignmentRole-MainUnit'::text AS reason
            FROM dbo.assignment a
                JOIN dbo.assignment a2 ON a.toid = a2.fromid
                JOIN dbo.role keyrole ON a2.roleid = keyrole.id AND keyrole.iskeyrole = true
                JOIN allRoles r ON a.roleid = r.roleid
            WHERE a.fromid IN (SELECT id FROM mainUnit) AND a2.toid = @toId
        ),
        rolemapRoles AS (
            -- Get roles through rolemap mappings for userRoles
            SELECT
                a.fromid,
                a.roleid        AS assroleid,
                a.viaId,
                a.viaroleid,
                a.toid,
                a.hasaccess,
                a.candelegate,
                r.roleid,
                r.roleurn,
                r.rolelegacyurn,
                r.isassignable,
                r.isdelegable,
                a.isassignmentrole,
                true AS isrolemap,
                a.isthroughkeyrole,
                a.isthroughmainunit,
                CONCAT(a.reason, '-RoleMap')::text AS reason
            FROM userRoles a
                JOIN dbo.rolemap rm ON rm.hasroleid = a.roleid
                JOIN allRoles r ON rm.getroleid = r.roleid
        ),
        allUserRoles AS (
            SELECT
                roleid,
                roleurn,
                rolelegacyurn,
                isassignable,
                isdelegable,
                assroleid,
                fromid,
                toid,
                viaid,
                viaroleid,
                hasaccess,
                candelegate,
                isassignmentrole,
                isrolemap,
                isthroughkeyrole,
                isthroughmainunit,
                false AS ismainadminrole,
                reason
            FROM userRoles ur
            WHERE candelegate = true

            UNION ALL

            SELECT
                roleid,
                roleurn,
                rolelegacyurn,
                isassignable,
                isdelegable,
                assroleid,
                fromid,
                toid,
                viaid,
                viaroleid,
                hasaccess,
                candelegate,
                isassignmentrole,
                isrolemap,
                isthroughkeyrole,
                isthroughmainunit,
                false AS ismainadminrole,
                reason
            FROM rolemapRoles rmr
            WHERE candelegate = true

            UNION ALL

            SELECT
                roleid,
                roleurn,
                rolelegacyurn,
                isassignable,
                isdelegable,
                NULL AS assroleid,
                NULL AS fromid,
                NULL AS toid,
                NULL AS viaid,
                NULL AS viaroleid,
                false AS hasaccess,
                true AS candelegate,
                NULL AS isassignmentrole,
                NULL AS isrolemap,
                NULL AS isthroughkeyrole,
                NULL AS isthroughmainunit,
                true AS ismainadminrole,
                'MainAdministratorRole'::text AS reason
            FROM mainAdminRoles admr
        )
        SELECT
            r.roleid,
            r.roleurn,
            r.rolelegacyurn,
            r.isassignable,
            r.isdelegable,
            assRole.id AS assignmentroleid,
            assRole.urn AS assignmentroleurn,
            fromid,
            fromEntity.name AS fromname,
            toid,
            toEntity.name AS toname,
            viaid,
            viaEntity.name AS vianame,
            viaroleid,
            viarole.urn AS viaroleurn,
            CASE WHEN hasaccess IS NULL THEN FALSE ELSE hasaccess END AS hasaccess,
            CASE WHEN candelegate IS NULL THEN FALSE ELSE candelegate END AS candelegate,
            CASE WHEN reason IS NULL THEN 'NoAccess' ELSE reason END AS reason,
            isassignmentrole,
            isrolemap,
            isthroughkeyrole,
            isthroughmainunit,
            ismainadminrole
        FROM allRoles r
            LEFT JOIN allUserRoles aur ON r.roleid = aur.roleid
            LEFT JOIN dbo.entity fromEntity ON aur.fromid = fromEntity.id
            LEFT JOIN dbo.entity toEntity ON aur.toid = toEntity.id
            LEFT JOIN dbo.entity viaEntity ON aur.viaid = viaEntity.id
            LEFT JOIN dbo.role viaRole ON aur.viaroleid = viaRole.id
            LEFT JOIN dbo.role assRole ON aur.assroleid = assRole.id
        """;
}
