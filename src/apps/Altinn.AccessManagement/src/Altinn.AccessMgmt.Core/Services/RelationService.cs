using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc />
public class NewRelationService(AppDbContext dbContext, DtoConverter dtoConverter) : INewRelationService
{
    /// <inheritdoc />
    public async Task<IEnumerable<RelationPackageDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Relations.AsNoTracking().Where(t => t.FromId == partyId);

        if (toId.HasValue)
        {
            query.Where(t => t.ToId == toId.Value);
        }

        if (roleId.HasValue)
        {
            query.Where(t => t.RoleId == roleId.Value);
        }

        if (packageId.HasValue)
        {
            query.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            query.Where(t => t.ResourceId == resourceId.Value);
        }

        var res = await query.ToListAsync(cancellationToken);

        return dtoConverter.ExtractRelationPackageDtoToOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RelationDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Relations.AsNoTracking().Where(t => t.FromId == partyId);

        if (toId.HasValue)
        {
            query.Where(t => t.ToId == toId.Value);
        }

        if (roleId.HasValue)
        {
            query.Where(t => t.RoleId == roleId.Value);
        }

        var res = await query.ToListAsync(cancellationToken);

        return dtoConverter.ExtractRelationDtoToOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RelationPackageDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Relations.AsNoTracking().Where(t => t.ToId == partyId);

        if (fromId.HasValue)
        {
            query.Where(t => t.FromId == fromId.Value);
        }

        if (roleId.HasValue)
        {
            query.Where(t => t.RoleId == roleId.Value);
        }

        if (packageId.HasValue)
        {
            query.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            query.Where(t => t.ResourceId == resourceId.Value);
        }

        var res = await query.ToListAsync(cancellationToken);

        return dtoConverter.ExtractRelationPackageDtoFromOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RelationDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Relations.AsNoTracking().Where(t => t.ToId == partyId);

        if (fromId.HasValue)
        {
            query.Where(t => t.FromId == fromId.Value);
        }

        if (roleId.HasValue)
        {
            query.Where(t => t.RoleId == roleId.Value);
        }

        var res = await query.ToListAsync(cancellationToken);

        return dtoConverter.ExtractRelationDtoFromOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermission>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var q = dbContext.Relations.AsNoTracking().Where(t => t.ToId == partyId);

        if (fromId.HasValue)
        {
            q.Where(t => t.FromId == fromId.Value);
        }

        if (packageId.HasValue)
        {
            q.Where(t => t.PackageId == packageId.Value);
        }

        var res = await q.ToListAsync(cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new PackagePermission()
            {
                Package = permission.Package,
                Permissions = packages.Where(t => t.Package.Id == permission.Package.Id).Select(dtoConverter.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermission>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var q = dbContext.Relations.AsNoTracking().Where(t => t.FromId == partyId);

        if (toId.HasValue)
        {
            q.Where(t => t.ToId == toId.Value);
        }

        if (packageId.HasValue)
        {
            q.Where(t => t.PackageId == packageId.Value);
        }

        var res = await q.ToListAsync(cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new PackagePermission()
            {
                Package = permission.Package,
                Permissions = packages.Where(t => t.Package.Id == permission.Package.Id).Select(dtoConverter.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var q = dbContext.Relations.AsNoTracking().Where(t => t.ToId == partyId);

        if (fromId.HasValue)
        {
            q.Where(t => t.FromId == fromId.Value);
        }

        if (packageId.HasValue)
        {
            q.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            q.Where(t => t.ResourceId == resourceId.Value);
        }

        var res = await q.ToListAsync(cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermission()
            {
                Resource = permission.Resource,
                Permissions = packages.Where(t => t.Resource.Id == permission.Resource.Id).Select(dtoConverter.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var q = dbContext.Relations.AsNoTracking().Where(t => t.FromId == partyId);

        if (toId.HasValue)
        {
            q.Where(t => t.ToId == toId.Value);
        }

        if (packageId.HasValue)
        {
            q.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            q.Where(t => t.ResourceId == resourceId.Value);
        }

        var res = await q.ToListAsync(cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermission()
            {
                Resource = permission.Resource,
                Permissions = packages.Where(t => t.Resource.Id == permission.Resource.Id).Select(dtoConverter.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackageDelegationCheck>> GetAssignablePackagePermissions(Guid partyId, Guid fromId, IEnumerable<Guid>? packageIds = null, CancellationToken cancellationToken = default)
    {
        /*
        @JK
        Flytt til egen service... PackageDelegationService?
        Lag en modell som er lik db resultat.
        Legg inn convert to Dto 
        */
        return await dbContext.Set<PackageDelegationCheck>().FromSqlRaw(AssignableAccessPackagesQuery(), partyId, fromId, packageIds).ToListAsync();
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

/// <summary>
/// Service for getting connections
/// </summary>
public interface INewRelationService
{
    /// <summary>
    /// Get Connections given from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="toId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="packageId">Filter for package</param>
    /// <param name="resourceId">Filter for resource</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationPackageDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Connections recived from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="fromId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="packageId">Filter for package</param>
    /// <param name="resourceId">Filter for resource</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationPackageDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connections given from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="toId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Connections recived from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="fromId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of packages with a list of parties you have this permission at
    /// </summary>
    Task<IEnumerable<PackagePermission>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of packages with for a party, where you have permission to assign the packages to others
    /// </summary>
    Task<IEnumerable<PackageDelegationCheck>> GetAssignablePackagePermissions(Guid partyId, Guid fromId, IEnumerable<Guid>? packageIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of packages with a list of parties that have this permission
    /// </summary>
    Task<IEnumerable<PackagePermission>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of resources with a list of parties you have this permission at
    /// </summary>
    Task<IEnumerable<ResourcePermission>> GetResourcePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of resources with a list of parties that have this permission
    /// </summary>
    Task<IEnumerable<ResourcePermission>> GetResourcePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);
}
