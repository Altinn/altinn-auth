using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc />
public class RelationService(IRelationRepository relationRepository, IRelationPermissionRepository relationPermissionRepository) : IRelationService
{
    /// <inheritdoc />
    public async Task<IEnumerable<Models.RelationDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, partyId);

        if (toId.HasValue)
        {
            filter.Equal(t => t.ToId, toId.Value);
        }

        if (roleId.HasValue)
        {
            filter.Equal(t => t.RoleId, roleId.Value);
        }

        if (packageId.HasValue)
        {
            filter.Equal(t => t.PackageId, packageId.Value);
        }

        if (resourceId.HasValue)
        {
            filter.Equal(t => t.ResourceId, resourceId.Value);
        }

        var res = await relationPermissionRepository.GetExtended(filter, cancellationToken: cancellationToken);

        return ExtractConnectionsToOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Models.CompactRelationDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, partyId);

        if (toId.HasValue)
        {
            filter.Equal(t => t.ToId, toId.Value);
        }

        if (roleId.HasValue)
        {
            filter.Equal(t => t.RoleId, roleId.Value);
        }

        var res = await relationRepository.GetExtended(filter, cancellationToken: cancellationToken);

        return ExtractConnectionsToOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Models.RelationDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, partyId);

        if (fromId.HasValue)
        {
            filter.Equal(t => t.FromId, fromId.Value);
        }

        if (roleId.HasValue)
        {
            filter.Equal(t => t.RoleId, roleId.Value);
        }

        if (packageId.HasValue)
        {
            filter.Equal(t => t.PackageId, packageId.Value);
        }

        if (resourceId.HasValue)
        {
            filter.Equal(t => t.ResourceId, resourceId.Value);
        }

        var res = await relationPermissionRepository.GetExtended(filter, cancellationToken: cancellationToken);

        return ExtractConnectionsFromOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Models.CompactRelationDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, partyId);

        if (fromId.HasValue)
        {
            filter.Equal(t => t.FromId, fromId.Value);
        }

        if (roleId.HasValue)
        {
            filter.Equal(t => t.RoleId, roleId.Value);
        }

        var res = await relationRepository.GetExtended(filter, cancellationToken: cancellationToken);

        return ExtractConnectionsFromOthers(res, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Models.PackagePermission>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, partyId);

        if (fromId.HasValue)
        {
            filter.Equal(t => t.FromId, fromId.Value);
        }

        if (packageId.HasValue)
        {
            filter.Equal(t => t.PackageId, packageId.Value);
        }

        var res = await relationPermissionRepository.GetExtended(filter, cancellationToken: cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new Models.PackagePermission()
            {
                Package = permission.Package,
                Permissions = packages.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Models.PackagePermission>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, partyId);

        if (toId.HasValue)
        {
            filter.Equal(t => t.ToId, toId.Value);
        }

        if (packageId.HasValue)
        {
            filter.Equal(t => t.PackageId, packageId.Value);
        }

        var res = await relationPermissionRepository.GetExtended(filter, cancellationToken: cancellationToken);
        if (res is { } && res.Any() && res.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new Models.PackagePermission()
            {
                Package = permission.Package,
                Permissions = packages.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Models.ResourcePermission>> GetResourcePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, partyId);

        if (fromId.HasValue)
        {
            filter.Equal(t => t.FromId, fromId.Value);
        }

        if (packageId.HasValue)
        {
            filter.Equal(t => t.PackageId, packageId.Value);
        }

        if (resourceId.HasValue)
        {
            filter.Equal(t => t.ResourceId, resourceId.Value);
        }

        var res = await relationPermissionRepository.GetExtended(filter, cancellationToken: cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Resource is { } && r.Package is { }) is var resources)
        {
            return resources.DistinctBy(t => t.Resource.Id).Select(permission => new Models.ResourcePermission()
            {
                Resource = permission.Resource,
                Permissions = resources.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Persistence.Models.PackageDelegationCheck>> GetAssignablePackagePermissions(Guid partyId, Guid fromId, IEnumerable<Guid>? packageIds = null, CancellationToken cancellationToken = default)
    {
        return await relationPermissionRepository.GetAssignableAccessPackages(fromId, partyId, packageIds, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Models.ResourcePermission>> GetResourcePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, partyId);

        if (toId.HasValue)
        {
            filter.Equal(t => t.ToId, toId.Value);
        }

        if (packageId.HasValue)
        {
            filter.Equal(t => t.PackageId, packageId.Value);
        }

        if (resourceId.HasValue)
        {
            filter.Equal(t => t.ResourceId, resourceId.Value);
        }

        var res = await relationPermissionRepository.GetExtended(filter, cancellationToken: cancellationToken);

        if (res is { } && res.Any() && res.Where(r => r.Resource is { } && r.Package is { }) is var resources)
        {
            return resources.DistinctBy(t => t.Resource.Id).Select(permission => new Models.ResourcePermission()
            {
                Resource = permission.Resource,
                Permissions = resources.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
            });
        }

        return [];
    }

    #region Extractors and Converters

    private IEnumerable<Models.CompactRelationDto> ExtractConnectionsToOthers(IEnumerable<ExtCompactRelation> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new Models.CompactRelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionToOthers(res, relation.To.Id).ToList() : new()
        });
    }

    private IEnumerable<Models.CompactRelationDto> ExtractSubConnectionToOthers(IEnumerable<ExtCompactRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new Models.CompactRelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<Models.RelationDto> ExtractConnectionsToOthers(IEnumerable<ExtRelation> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new Models.RelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionToOthers(res, relation.To.Id).ToList() : new()
        });
    }

    private IEnumerable<Models.RelationDto> ExtractSubConnectionToOthers(IEnumerable<ExtRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new Models.RelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<Models.CompactRelationDto> ExtractConnectionsFromOthers(IEnumerable<ExtCompactRelation> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new Models.CompactRelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    private IEnumerable<Models.RelationDto> ExtractConnectionsFromOthers(IEnumerable<ExtRelation> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new Models.RelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    private IEnumerable<Models.CompactRelationDto> ExtractSubConnectionFromOthers(IEnumerable<ExtCompactRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new Models.CompactRelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<Models.RelationDto> ExtractSubConnectionFromOthers(IEnumerable<ExtRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new Models.RelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private Models.Permission ConvertToPermission(ExtRelation connection)
    {
        return new Models.Permission()
        {
            From = connection.From,
            To = connection.To,
            Via = connection.Via,
            ViaRole = connection.ViaRole,
            Role = connection.Role
        };
    }

    #endregion
}
