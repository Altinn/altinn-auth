using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
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

    public static CompactEntityDto Convert(Entity entity, bool isConvertingParent = false)
    {
        if (entity is { })
        {
            EntityTypeConstants.TryGetById(entity.TypeId, out var type);
            EntityVariantConstants.TryGetById(entity.VariantId, out var variant);

            return new CompactEntityDto()
            {
                Id = entity.Id,
                Name = entity?.Name,
                Type = type.Entity.Name,
                Variant = variant.Entity.Name,
                Parent = isConvertingParent ? null : Convert(entity.Parent, true),
                Children = null,
                DateOfBirth = entity.DateOfBirth,
                DateOfDeath = entity.DateOfDeath,
                IsDeleted = entity.IsDeleted,
                DeletedAt = entity.DeletedAt,
                OrganizationIdentifier = entity.OrganizationIdentifier,
                PartyId = entity.PartyId,
                PersonIdentifier = entity.PersonIdentifier,
                UserId = entity.UserId,
                Username = entity.Username
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
                Code = role.Code,
                Urn = role.Urn,
                LegacyUrn = role.LegacyUrn
            };
        }

        return null;
    }

    public static CompactPackageDto ConvertCompactPackage(ConnectionQueryPackage package)
    {
        if (package is { })
        {
            return new CompactPackageDto()
            {
                Id = package.Id,
                Urn = package.Urn,
                AreaId = package.AreaId,
            };
        }

        return null;
    }

    public static CompactResourceDto ConvertCompactResource(ConnectionQueryResource resource)
    {
        if (resource is { })
        {
            return new CompactResourceDto()
            {
                Id = resource.Id,
                Value = resource.Name
            };
        }

        return null;
    }

    public static CompactResourceDto ConvertCompactResource(Resource resource)
    {
        if (resource is { })
        {
            return new CompactResourceDto()
            {
                Id = resource.Id,
                Value = resource.Name
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
