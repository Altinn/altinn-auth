using Altinn.AccessMgmt.PersistenceEF.Constants;
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
                KeyValues = GetFakeKeyValues(entity),
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

    private static Dictionary<string, string> GetFakeKeyValues(Entity entity)
    {
        var result = new Dictionary<string, string>();

        if (entity.TypeId.Equals(EntityTypeConstants.Organisation))
        {
            result.Add("OrganizationIdentifier", entity.RefId);
        }

        if (entity.TypeId.Equals(EntityTypeConstants.Person))
        {
            result.Add("PersonIdentifier", entity.RefId);
            if (!string.IsNullOrEmpty(entity.RefId) && entity.RefId.Length >= 6)
            {
                result.Add("DateOfBirth", CalculateDateOfBirth(entity.RefId).Value.ToString("yyyy-MM-dd"));
            }
        }

        return result;
    }

    private static DateOnly? CalculateDateOfBirth(string personIdentifier)
    {
        var s = personIdentifier.AsSpan();
        var d1 = s[0] - '0';
        var d2 = s[1] - '0';
        var m1 = s[2] - '0';
        var m2 = s[3] - '0';
        var y1 = s[4] - '0';
        var y2 = s[5] - '0';
        var i1 = s[6] - '0';
        var i2 = s[7] - '0';
        var i3 = s[8] - '0';

        var dayComponent = (d1 * 10) + d2;
        var monthComponent = (m1 * 10) + m2;
        var yearComponent = (y1 * 10) + y2;
        var individualComponent = (i1 * 100) + (i2 * 10) + i3;

        if (monthComponent >= 80)
        {
            // Test person
            monthComponent -= 80;
        }

        if (dayComponent >= 40)
        {
            // D-number
            dayComponent -= 40;
        }

        var year = individualComponent switch
        {
            >= 500 and < 750 when yearComponent > 54 => 1800 + yearComponent,
            >= 900 when yearComponent > 39 => 1900 + yearComponent,
            >= 500 when yearComponent < 40 => 2000 + yearComponent,
            _ => 1900 + yearComponent,
        };

        try
        {
            return new DateOnly(year, monthComponent, dayComponent);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Invalid date of birth
            return null;
        }
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
