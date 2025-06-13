using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc />
public class RelationService(IRelationRepository relationRepository, IRelationPermissionRepository relationPermissionRepository) : IRelationService
{
    /// <inheritdoc />
    public async Task<IEnumerable<RelationDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
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
    public async Task<IEnumerable<CompactRelationDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
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
    public async Task<IEnumerable<RelationDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
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
    public async Task<IEnumerable<CompactRelationDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
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
    public async Task<IEnumerable<PackagePermission>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
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

        return res.Where(r => r.Package is { }).DistinctBy(t => t.Package.Id).Select(permission => new PackagePermission()
        {
            Package = permission.Package,
            Permissions = res.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermission>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
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

        return res.Where(r => r.Package is { }).DistinctBy(t => t.Package.Id).Select(permission => new PackagePermission()
        {
            Package = permission.Package,
            Permissions = res.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
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

        return res.Where(r => r.Resource is { }).DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermission()
        {
            Resource = permission.Resource,
            Permissions = res.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
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

        return res.Where(r => r.Resource is { }).DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermission()
        {
            Resource = permission.Resource,
            Permissions = res.Where(t => t.Package.Id == permission.Package.Id).Select(ConvertToPermission)
        });
    }

    #region Extractors and Converters

    private IEnumerable<CompactRelationDto> ExtractConnectionsToOthers(IEnumerable<ExtCompactRelation> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new CompactRelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionToOthers(res, relation.To.Id).ToList() : new()
        });
    }

    private IEnumerable<CompactRelationDto> ExtractSubConnectionToOthers(IEnumerable<ExtCompactRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new CompactRelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<RelationDto> ExtractConnectionsToOthers(IEnumerable<ExtRelation> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new RelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionToOthers(res, relation.To.Id).ToList() : new()
        });
    }

    private IEnumerable<RelationDto> ExtractSubConnectionToOthers(IEnumerable<ExtRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new RelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<CompactRelationDto> ExtractConnectionsFromOthers(IEnumerable<ExtCompactRelation> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new CompactRelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    private IEnumerable<RelationDto> ExtractConnectionsFromOthers(IEnumerable<ExtRelation> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new RelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubConnectionFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    private IEnumerable<CompactRelationDto> ExtractSubConnectionFromOthers(IEnumerable<ExtCompactRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new CompactRelationDto() 
        { 
            Party = relation.From, 
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(), 
            Connections = new() 
        });
    }

    private IEnumerable<RelationDto> ExtractSubConnectionFromOthers(IEnumerable<ExtRelation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new RelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private CompactPermission ConvertToCompactPermission(ExtRelation connection)
    {
        return new CompactPermission()
        {
            From = connection.From,
            To = connection.To
        };
    }

    private Permission ConvertToPermission(ExtRelation connection)
    {
        return new Permission()
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
