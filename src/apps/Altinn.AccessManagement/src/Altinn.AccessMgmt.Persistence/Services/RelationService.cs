using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc />
public class RelationService(IRelationRepository relationRepository, IRelationPermissionRepository relationPermissionRepository) : IRelationService
{
    /// <inheritdoc />
    public async Task<IEnumerable<RelationDto>> GetConnectionsFrom(Guid partyId, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, partyId);

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

        var res = await relationPermissionRepository.GetExtended(filter);

        return GetConnectionsFrom(res);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CompactRelationDto>> GetConnectionsFrom(Guid partyId, Guid? roleId = null)
    {
        var filter = relationRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, partyId);

        if (roleId.HasValue)
        {
            filter.Equal(t => t.RoleId, roleId.Value);
        }

        var res = await relationRepository.GetExtended(filter);

        return GetConnectionsFrom(res);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RelationDto>> GetConnectionsTo(Guid partyId, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, partyId);

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

        var res = await relationPermissionRepository.GetExtended(filter);

        return GetConnectionsTo(res);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CompactRelationDto>> GetConnectionsTo(Guid partyId, Guid? roleId = null)
    {
        var filter = relationRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, partyId);

        if (roleId.HasValue)
        {
            filter.Equal(t => t.RoleId, roleId.Value);
        }

        var res = await relationRepository.GetExtended(filter);

        return GetConnectionsTo(res);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionPermission>> GetPackagePermissionsFrom(Guid partyId, Guid packageId)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, partyId);
        filter.Equal(t => t.PackageId, packageId);
        var res = await relationPermissionRepository.GetExtended(filter);

        var result = new List<ConnectionPermission>();

        foreach (var connection in res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(t => t.To))
        {
            var perm = new ConnectionPermission()
            {
                Party = connection,
                KeyRoles = new List<Permission>(),
                Delegations = new List<Permission>()
            };

            perm.KeyRoles = res.Where(t => t.Reason == "KeyRole" && t.Via.Id == connection.Id).Select(t => ConvertToPermission(t)).ToList();
            perm.Delegations = res.Where(t => t.Reason == "Delegation" && t.Via.Id == connection.Id).Select(t => ConvertToPermission(t)).ToList();

            result.Add(perm);
        }

        return result.DistinctBy(t => t.Party.Id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionPermission>> GetPackagePermissionsTo(Guid partyId, Guid packageId)
    {
        var filter = relationPermissionRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, partyId);
        filter.Equal(t => t.PackageId, packageId);
        var res = await relationPermissionRepository.GetExtended(filter);

        var result = new List<ConnectionPermission>();

        foreach (var connection in res.Where(t => t.Reason == "Direct").DistinctBy(t => t.From.Id).Select(t => t.From))
        {
            var perm = new ConnectionPermission()
            {
                Party = connection,
                KeyRoles = new List<Permission>(),
                Delegations = new List<Permission>()
            };

            perm.KeyRoles = res.Where(t => t.Reason == "KeyRole" && t.Via.Id == connection.Id).Select(t => ConvertToPermission(t)).ToList();
            perm.Delegations = res.Where(t => t.Reason == "Delegation" && t.Via.Id == connection.Id).Select(t => ConvertToPermission(t)).ToList();

            result.Add(perm);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CompactPackage>> GetPackagesFrom(Guid partyId, Guid? toId = null, Guid? packageId = null)
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

        var res = await relationPermissionRepository.GetExtended(filter);

        return res.Select(t => t.Package);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CompactPackage>> GetPackagesTo(Guid partyId, Guid? fromId = null, Guid? packageId = null)
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

        var res = await relationPermissionRepository.GetExtended(filter);

        return res.Select(t => t.Package);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CompactResource>> GetResourcesFrom(Guid partyId, Guid? toId = null, Guid? packageId = null)
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

        var res = await relationPermissionRepository.GetExtended(filter);

        return res.Select(t => t.Resource);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CompactResource>> GetResourcesTo(Guid partyId, Guid? fromId = null, Guid? packageId = null)
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

        var res = await relationPermissionRepository.GetExtended(filter);

        return res.Select(t => t.Resource);
    }

    private IEnumerable<CompactRelationDto> GetConnectionsFrom(IEnumerable<ExtCompactRelation> res)
    {
        var result = new List<CompactRelationDto>();

        var tempResult = new Dictionary<Guid, CompactRelationDto>(); // ViaId - Include Reason for Split in KeyRole & Delegation (?)
        foreach (var connection in res.Where(t => t.Reason != "Direct").DistinctBy(t => t.To.Id))
        {
            var party = connection.To;
            tempResult.Add(connection.Via.Id, new CompactRelationDto()
            {
                Party = party,
                Roles = res.Where(t => t.To.Id == party.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Connections = new List<CompactRelationDto>(),
            });
        }

        foreach (var party in res.Where(t => t.Reason == "Direct").Select(t => t.To).DistinctBy(t => t.Id))
        {
            result.Add(new CompactRelationDto()
            {
                Party = party,
                Roles = res.Where(t => t.To.Id == party.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Connections = tempResult.Where(t => t.Key == party.Id).Select(t => t.Value).DistinctBy(t => t.Party.Id).ToList(), // Split in KeyRole & Delegation (?)
            });
        }

        return result;
    }

    private IEnumerable<RelationDto> GetConnectionsFrom(IEnumerable<ExtRelation> res)
    {
        var result = new List<RelationDto>();

        var tempResult = new Dictionary<Guid, RelationDto>(); // ViaId - Include Reason for Split in KeyRole & Delegation (?)
        foreach (var connection in res.Where(t => t.Reason != "Direct").DistinctBy(t => t.To.Id))
        {
            var party = connection.To;
            tempResult.Add(connection.Via.Id, new RelationDto()
            {
                Party = party,
                Roles = res.Where(t => t.To.Id == party.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Packages = res.Where(t => t.To.Id == party.Id).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
                Connections = new List<RelationDto>(),
            });
        }

        foreach (var party in res.Where(t => t.Reason == "Direct").Select(t => t.To).DistinctBy(t => t.Id))
        {
            result.Add(new RelationDto()
            {
                Party = party,
                Roles = res.Where(t => t.To.Id == party.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Packages = res.Where(t => t.To.Id == party.Id).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
                Connections = tempResult.Where(t => t.Key == party.Id).Select(t => t.Value).DistinctBy(t => t.Party.Id).ToList(), // Split in KeyRole & Delegation (?)
            });
        }

        return result;
    }

    private IEnumerable<CompactRelationDto> GetConnectionsTo(IEnumerable<ExtCompactRelation> res)
    {
        var result = new List<CompactRelationDto>();

        var tempResult = new Dictionary<Guid, CompactRelationDto>(); // ViaId - Include Reason for Split in KeyRole & Delegation (?)
        foreach (var connection in res.Where(t => t.Reason != "Direct").DistinctBy(t => t.From.Id))
        {
            var fromParty = connection.From;
            tempResult.Add(connection.Via.Id, new CompactRelationDto()
            {
                Party = fromParty,
                Roles = res.Where(t => t.From.Id == fromParty.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Connections = new List<CompactRelationDto>(),
            });
        }

        foreach (var party in res.Where(t => t.Reason == "Direct").Select(t => t.From).DistinctBy(t => t.Id))
        {
            result.Add(new CompactRelationDto()
            {
                Party = party,
                Roles = res.Where(t => t.From.Id == party.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Connections = tempResult.Where(t => t.Key == party.Id).Select(t => t.Value).DistinctBy(t => t.Party.Id).ToList(), // Split in KeyRole & Delegation (?)
            });
        }

        return result;
    }

    private IEnumerable<RelationDto> GetConnectionsTo(IEnumerable<ExtRelation> res)
    {
        var result = new List<RelationDto>();

        var tempResult = new Dictionary<Guid, RelationDto>(); // ViaId
        foreach (var connection in res.Where(t => t.Reason != "Direct").DistinctBy(t => t.From.Id))
        {
            var fromParty = connection.From;
            tempResult.Add(connection.Via.Id, new RelationDto()
            {
                Party = fromParty,
                Roles = res.Where(t => t.From.Id == fromParty.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Packages = res.Where(t => t.From.Id == fromParty.Id).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
                Connections = new List<RelationDto>(),
            });
        }

        foreach (var party in res.Where(t => t.Reason == "Direct").Select(t => t.From).DistinctBy(t => t.Id))
        {
            result.Add(new RelationDto()
            {
                Party = party,
                Roles = res.Where(t => t.From.Id == party.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
                Packages = res.Where(t => t.From.Id == party.Id).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
                Connections = tempResult.Where(t => t.Key == party.Id).Select(t => t.Value).DistinctBy(t => t.Party.Id).ToList(),
            });
        }

        return result;
    }

    private Permission ConvertToPermission(ExtCompactRelation connection)
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
}
