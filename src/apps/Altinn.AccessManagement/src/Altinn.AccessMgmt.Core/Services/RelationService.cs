using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public class RelationService(AppDbContext dbContext, DtoMapper dtoConverter) : IRelationService
{
    /// <inheritdoc />
    public async Task<IEnumerable<RelationPackageDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Relations.AsNoTracking().Where(t => t.FromId == partyId);

        if (toId.HasValue)
        {
            query = query.Where(t => t.ToId == toId.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(t => t.RoleId == roleId.Value);
        }

        if (packageId.HasValue)
        {
            query = query.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            query = query.Where(t => t.ResourceId == resourceId.Value);
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
            query = query.Where(t => t.ToId == toId.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(t => t.RoleId == roleId.Value);
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
            query = query = query.Where(t => t.FromId == fromId.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(t => t.RoleId == roleId.Value);
        }

        if (packageId.HasValue)
        {
            query = query.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            query = query.Where(t => t.ResourceId == resourceId.Value);
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
            query = query.Where(t => t.FromId == fromId.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(t => t.RoleId == roleId.Value);
        }

        var res = await query.ToListAsync(cancellationToken);

        return dtoConverter.ExtractRelationDtoFromOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var q = dbContext.Relations.AsNoTracking().Include(t => t.Package).Where(t => t.ToId == partyId);

        if (fromId.HasValue)
        {
            q = q.Where(t => t.FromId == fromId.Value);
        }

        if (packageId.HasValue)
        {
            q = q.Where(t => t.PackageId == packageId.Value);
        }

        var res = await q.ToListAsync(cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new PackagePermissionDto()
            {
                Package = permission.Package,
                Permissions = packages.Where(t => t.Package.Id == permission.Package.Id).Select(dtoConverter.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var q = dbContext.Relations.AsNoTracking().Where(t => t.FromId == partyId);

        if (toId.HasValue)
        {
            q = q.Where(t => t.ToId == toId.Value);
        }

        if (packageId.HasValue)
        {
            q = q.Where(t => t.PackageId == packageId.Value);
        }

        var res = await q.ToListAsync(cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new PackagePermissionDto()
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
            q = q.Where(t => t.FromId == fromId.Value);
        }

        if (packageId.HasValue)
        {
            q = q.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            q = q.Where(t => t.ResourceId == resourceId.Value);
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
            q = q.Where(t => t.ToId == toId.Value);
        }

        if (packageId.HasValue)
        {
            q = q.Where(t => t.PackageId == packageId.Value);
        }

        if (resourceId.HasValue)
        {
            q = q.Where(t => t.ResourceId == resourceId.Value);
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
}
