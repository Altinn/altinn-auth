using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Dto Mapping
/// </summary>
public partial class DtoMapper
{
    public IEnumerable<ConnectionPackageDto> ExtractRelationPackageDtoToOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.ToId).Select(relation => new ConnectionPackageDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.ToId == relation.ToId).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.ToId == relation.ToId && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.ToId).ToList() : new()
        });
    }
    
    public IEnumerable<ConnectionPackageDto> ExtractSubRelationPackageDtoFromOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.FromId).Select(relation => new ConnectionPackageDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.FromId == relation.FromId && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractSubRelationDtoFromOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.FromId).Select(relation => new ConnectionDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractRelationDtoToOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.ToId).Select(relation => new ConnectionDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.ToId == relation.ToId).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.ToId).ToList() : new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractSubRelationDtoToOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.To.Id).Select(relation => new ConnectionDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.ToId == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionPackageDto> ExtractSubRelationPackageDtoToOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.ToId).Select(relation => new ConnectionPackageDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.ToId == relation.ToId).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.ToId == relation.ToId && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractRelationDtoFromOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.FromId).Select(relation => new ConnectionDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.FromId).ToList() : new()
        });
    }

    public IEnumerable<ConnectionPackageDto> ExtractRelationPackageDtoFromOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.FromId).Select(relation => new ConnectionPackageDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.FromId == relation.FromId && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.FromId).ToList() : new()
        });
    }

    public CompactPermission ConvertToCompactPermission(Connection connection)
    {
        return new CompactPermission()
        {
            From = connection.From,
            To = connection.To
        };
    }

    public PermissionDto ConvertToPermission(Connection connection)
    {
        return new PermissionDto()
        {
            From = connection.From,
            To = connection.To,
            Via = connection.Via,
            ViaRole = connection.ViaRole,
            Role = connection.Role
        };
    }
}
