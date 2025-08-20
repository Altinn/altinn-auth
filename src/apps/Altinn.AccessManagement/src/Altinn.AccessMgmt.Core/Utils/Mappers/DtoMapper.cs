using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Dto Mapping
/// </summary>
public partial class DtoMapper
{
    public IEnumerable<RelationPackageDto> ExtractRelationPackageDtoToOthers(IEnumerable<Relation> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new RelationPackageDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.To.Id).ToList() : new()
        });
    }
    
    public IEnumerable<RelationPackageDto> ExtractSubRelationPackageDtoFromOthers(IEnumerable<Relation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new RelationPackageDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<RelationDto> ExtractSubRelationDtoFromOthers(IEnumerable<Relation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new RelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<RelationDto> ExtractRelationDtoToOthers(IEnumerable<Relation> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new RelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.To.Id).ToList() : new()
        });
    }

    public IEnumerable<RelationDto> ExtractSubRelationDtoToOthers(IEnumerable<Relation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new RelationDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<RelationPackageDto> ExtractSubRelationPackageDtoToOthers(IEnumerable<Relation> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new RelationPackageDto()
        {
            Party = relation.To,
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }





    public IEnumerable<RelationDto> ExtractRelationDtoFromOthers(IEnumerable<Relation> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new RelationDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    public IEnumerable<RelationPackageDto> ExtractRelationPackageDtoFromOthers(IEnumerable<Relation> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new RelationPackageDto()
        {
            Party = relation.From,
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => t.Role).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => t.Package).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    public CompactPermission ConvertToCompactPermission(Relation connection)
    {
        return new CompactPermission()
        {
            From = connection.From,
            To = connection.To
        };
    }

    public PermissionDto ConvertToPermission(Relation connection)
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
