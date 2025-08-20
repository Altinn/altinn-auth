using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class DelegationCheckService(AppDbContext dbContext) : IDelegationCheckService
{
    /// <inheritdoc/>
    public async Task<IEnumerable<AccessPackageDto.Check>> GetAssignableAccessPackages(Guid toId, Guid fromId, IEnumerable<Guid> packageIds, CancellationToken cancellationToken)
    {
        var res = await dbContext.Set<PackageDelegationCheck>().FromSqlRaw(AssignableAccessPackagesQuery(), toId, fromId, packageIds).ToListAsync();

        if (res is { } && res.Any())
        {
            return res.Select(DtoMapper.Convert);
        }

        return [];
    }

    private static string AssignableAccessPackagesQuery()
    {
        return $"""
        WITH mainUnit AS (	
        	-- Get main unit of from-party if exists
            SELECT 
        		a.toid AS id
        	FROM dbo.assignment a
        		JOIN dbo.role r ON a.roleid = r.id
        	WHERE a.fromid = @fromId
        		AND r.code IN ('hovedenhet', 'ikke-naeringsdrivende-hovedenhet')
        ),
        allPackages AS (
        	SELECT 
        	    p.id AS packageid,
        	    p.urn AS packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable
        	FROM dbo.package p
        ),
        mainAdminPackages AS (
        	-- Get all packages for main administrator
            SELECT 
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable	    
        	FROM dbo.rolepackage rp
        		JOIN dbo.role r ON rp.roleid = r.id
        		JOIN allPackages p ON rp.packageid = p.packageid
        	WHERE r.code = 'hovedadministrator'
        ),
        userPackages AS (
        	-- Get directy delegated packages to user/party
        	SELECT
                a.fromid,
        	    a.roleid,
        	    NULL::uuid     	AS viaid, 
        	    NULL::uuid     	AS viaroleid,
        	    a.toid,
        	    true     		AS hasaccess,
        	    true     		AS candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    true AS isassignmentpackage,
        	    false AS isrolepackage,
        	    false AS iskeyrolepackage,
        	    false AS ismainunitpackage,
        	    'Direct-AssignmentPackage'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        	    JOIN allPackages p ON ap.packageid = p.packageid
        	WHERE a.fromid = @fromId AND a.toid = @toId

        	UNION ALL

        	-- Get packages through role-assignments to user/party
        	SELECT
                a.fromid,
        	    a.roleid,
        	    NULL::uuid     AS viaid,
        	    NULL::uuid     AS viaroleid,
        	    a.toid,
        	    rp.hasaccess,
        	    rp.candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    false AS isassignmentpackage,
        	    true AS isrolepackage,
        	    false AS iskeyrolepackage,
        	    false AS ismainunitpackage,
        	    'Direct-RolePackage'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.rolepackage rp ON rp.roleid = a.roleid
        	    JOIN allPackages p ON rp.packageid = p.packageid
        	WHERE a.fromid = @fromId AND a.toid = @toId

        	UNION ALL

        	-- Get packages directy delegated to key-role relations of the user/party
        	SELECT
                a.fromid,
        	    a.roleid,
        	    a2.fromid       AS viaid,
        	    a2.roleid       AS viaroleid,
        	    a2.toid,
        	    true     		AS hasaccess,
        	    true     		AS candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    true AS isassignmentpackage,
        	    false AS isrolepackage,
        	    true AS iskeyrolepackage,
        	    false AS ismainunitpackage,
        	    'KeyRole-AssignmentPackage'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.assignment a2 ON a.toid = a2.fromid
        	    JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
        	    JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        	    JOIN allPackages p ON ap.packageid = p.packageid
        	WHERE a.fromid = @fromId AND a2.toid = @toId

        	UNION ALL

        	-- Get packages through role-assignments to key-role relations of the user/party
        	SELECT
                a.fromid,
        	    a.roleid,
        	    a2.fromid       AS viaid,
        	    a2.roleid       AS viaroleid,
        	    a2.toid,
        	    rp.hasaccess,
        	    rp.candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    false AS isassignmentpackage,
        	    true AS isrolepackage,
        	    true AS iskeyrolepackage,
        	    false AS ismainunitpackage,
        	    'KeyRole-RolePackage'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.assignment a2 ON a.toid = a2.fromid
        	    JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
        	    JOIN dbo.rolepackage rp ON rp.roleid = a.roleid
        	    JOIN allPackages p ON rp.packageid = p.packageid
        	WHERE a.fromid = @fromId AND a2.toid = @toId

        	UNION ALL

        	-- Get directy delegated packages to user/party from main unit
        	SELECT
                a.fromid,
        	    a.roleid,
        	    NULL::uuid      AS viaid,
        	    NULL::uuid      AS viaroleid,
        	    a.toid,
        	    true     		AS hasaccess,
        	    true     		AS candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    true AS isassignmentpackage,
        	    false AS isrolepackage,
        	    false AS iskeyrolepackage,
        	    true AS ismainunitpackage,
        	    'Direct-AssignmentPackage-MainUnit'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        	    JOIN allPackages p ON ap.packageid = p.packageid
        	WHERE a.fromid IN (SELECT id FROM mainUnit) AND a.toid = @toId

        	UNION ALL

        	-- Get packages through role-assignments to user/party from main unit
        	SELECT
                a.fromid,
        	    a.roleid,
        	    NULL::uuid     AS viaid,
        	    NULL::uuid     AS viaroleid,
        	    a.toid,
        	    rp.hasaccess,
        	    rp.candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    false AS isassignmentpackage,
        	    true AS isrolepackage,
        	    false AS iskeyrolepackage,
        	    true AS ismainunitpackage,
        	    'Direct-RolePackage-MainUnit'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.rolepackage rp ON rp.roleid = a.roleid
        	    JOIN allPackages p ON rp.packageid = p.packageid
        	WHERE a.fromid IN (SELECT id FROM mainUnit) AND a.toid = @toId

        	UNION ALL

        	-- Get packages directy delegated to key-role relations of the user/party from main unit
        	SELECT
                a.fromid,
        	    a.roleid,
        	    a2.fromid       AS viaid,
        	    a2.roleid       AS viaroleid,
        	    a2.toid,
        	    true     		AS hasaccess,
        	    true     		AS candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    true AS isassignmentpackage,
        	    false AS isrolepackage,
        	    true AS iskeyrolepackage,
        	    true AS ismainunitpackage,
        	    'KeyRole-AssignmentPackage-MainUnit'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.assignment a2 ON a.toid = a2.fromid
        	    JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
        	    JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        	    JOIN allPackages p ON ap.packageid = p.packageid
        	WHERE a.fromid IN (SELECT id FROM mainUnit) AND a2.toid = @toId

        	UNION ALL

        	-- Get packages through role-assignments to key-role relations of the user/party from main unit
        	SELECT
                a.fromid,
        	    a.roleid,
        	    a2.fromid       AS viaid,
        	    a2.roleid       AS viaroleid,
        	    a2.toid,
        	    rp.hasaccess,
        	    rp.candelegate,
        	    p.packageid,
        	    p.packageurn,
        	    p.areaid,
        	    p.isassignable,
        	    p.isdelegable,
        	    false AS isassignmentpackage,
        	    true AS isrolepackage,
        	    true AS iskeyrolepackage,
        	    true AS ismainunitpackage,
        	    'KeyRole-RolePackage-MainUnit'::text AS reason
        	FROM dbo.assignment a
        	    JOIN dbo.assignment a2 ON a.toid = a2.fromid
        	    JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
        	    JOIN dbo.rolepackage rp ON rp.roleid = a.roleid
        	    JOIN allPackages p ON rp.packageid = p.packageid
        	WHERE a.fromid IN (SELECT id FROM mainUnit) AND a2.toid = @toId
        ),
        allAssignableUserPackages AS (
        	SELECT
        		packageid,
        		packageurn,
        		areaid,
        		isassignable,
        		isdelegable,
        		roleid,
        		fromid,
        		toid,
        		viaid,
        		viaroleid,
        		hasaccess,
        		candelegate,
        		isassignmentpackage,
        	    isrolepackage,
        	    iskeyrolepackage,
        	    ismainunitpackage,
        		false AS ismainadminpackage,
        		reason
        	FROM userpackages up
        	WHERE candelegate = true
        		AND isassignable = TRUE
        		AND (packageurn != 'urn:altinn:accesspackage:hovedadministrator' OR isrolepackage = true)	

        	UNION ALL

        	SELECT
        		admp.packageid,
        		admp.packageurn,
        		admp.areaid,
        		admp.isassignable,
        		admp.isdelegable,
        		roleid,
        		fromid,
        		toid,
        		viaid,
        		viaroleid,		
        		hasaccess,
        		candelegate,
        		isassignmentpackage,
                isrolepackage,
                iskeyrolepackage,
                ismainunitpackage,
        		true AS ismainadminpackage,
        		CONCAT(reason, '-HovedAdmin')
        	FROM userpackages up
        		CROSS JOIN mainAdminPackages admp
        	WHERE up.packageurn = 'urn:altinn:accesspackage:hovedadministrator'
        		AND admp.isassignable = true
        )
        SELECT
        	p.packageid,
        	p.packageurn,
        	p.areaid,
        	p.isassignable,
        	p.isdelegable,
        	roleid,
        	r.urn AS roleurn,
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
        	isassignmentpackage,
        	isrolepackage,
        	iskeyrolepackage,
        	ismainunitpackage,
        	ismainadminpackage
        FROM allPackages p
        	LEFT JOIN allAssignableUserPackages aaup ON p.packageid = aaup.packageid
        	LEFT JOIN dbo.role r ON aaup.roleid = r.id
        	LEFT JOIN dbo.entity fromEntity ON fromid = fromEntity.id
        	LEFT JOIN dbo.entity toEntity ON toid = toEntity.id
        	LEFT JOIN dbo.entity viaEntity ON viaid = viaEntity.id
        	LEFT JOIN dbo.role viaRole ON viaroleid = viaRole.id
        WHERE
            (array_length(@packageIds, 1) IS NULL OR p.packageid = ANY(@packageIds))
        """;
    }
}
