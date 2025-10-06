using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper { }

/// <summary>
/// The DtoMapper is a partial class for converting database models and dto models
/// Create a new file for the diffrent areas
/// </summary>
public partial class DtoMapper
{
    public IEnumerable<ConnectionPackageDto> ExtractRelationPackageDtoToOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new ConnectionPackageDto()
        {
            Party = Convert(relation.To),
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.To.Id).ToList() : new()
        });
    }

    public IEnumerable<ConnectionPackageDto> ExtractSubRelationPackageDtoFromOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new ConnectionPackageDto()
        {
            Party = Convert(relation.From),
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractSubRelationDtoFromOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.From.Id).Select(relation => new ConnectionDto()
        {
            Party = Convert(relation.From),
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractRelationDtoToOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.To.Id).Select(relation => new ConnectionDto()
        {
            Party = Convert(relation.To),
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.To.Id).ToList() : new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractSubRelationDtoToOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new ConnectionDto()
        {
            Party = Convert(relation.To),
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionPackageDto> ExtractSubRelationPackageDtoToOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.Via.Id == party).DistinctBy(t => t.To.Id).Select(relation => new ConnectionPackageDto()
        {
            Party = Convert(relation.To),
            Roles = res.Where(t => t.To.Id == relation.To.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.To.Id == relation.To.Id && t.Package != null).Select(t => ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    public IEnumerable<ConnectionDto> ExtractRelationDtoFromOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new ConnectionDto()
        {
            Party = Convert(relation.From),
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    public IEnumerable<ConnectionPackageDto> ExtractRelationPackageDtoFromOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.From.Id).Select(relation => new ConnectionPackageDto()
        {
            Party = Convert(relation.From),
            Roles = res.Where(t => t.From.Id == relation.From.Id).Select(t => ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.From.Id == relation.From.Id && t.Package != null).Select(t => ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.From.Id).ToList() : new()
        });
    }

    public static CompactEntityDto Convert(Entity compactEntity, bool isConvertingParent = false)
    {
        if (compactEntity is { })
        {
            return new CompactEntityDto()
            {
                Id = compactEntity.Id,
                Name = compactEntity?.Name,
                Type = compactEntity?.Type?.Name,
                Variant = compactEntity?.Variant?.Name,
                Parent = isConvertingParent ? null : Convert(compactEntity.Parent, true),
                Children = null
            };
        }

        return null;
    }

    public static CompactRoleDto ConvertCompactRole(Role role)
    {
        if (role is { })
        {
            return new CompactRoleDto()
            {
                Id = role.Id,
                Children = null,
                Code = role.Code
            };
        }

        return null;
    }

    public static CompactPackageDto ConvertCompactPackage(Package package)
    {
        if (package is { })
        {
            return new CompactPackageDto()
            {
                Id = package.Id,
                AreaId = package.AreaId,
                Urn = package.Urn
            };
        }

        return null;
    }
}

public interface IDtoMapper { }
