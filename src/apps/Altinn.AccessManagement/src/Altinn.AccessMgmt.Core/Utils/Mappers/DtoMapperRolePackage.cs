using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static PackageDto Convert(Package obj)
    {
        return new PackageDto()
        {
            Id = obj.Id,
            Name = obj.Name,
            Urn = obj.Urn,
            Description = obj.Description,
            IsDelegable = obj.IsDelegable,
            IsAssignable = obj.IsAssignable
        };
    }

    public static PackageDto Convert(Package obj, Area area, IEnumerable<Resource> resources)
    {
        return new PackageDto()
        {
            Id = obj.Id,
            Name = obj.Name,
            Urn = obj.Urn,
            Description = obj.Description,
            IsDelegable = obj.IsDelegable,
            IsAssignable = obj.IsAssignable,
            Area = area,
            Resources = resources
        };
    }

    public static RolePackageDto Convert(RolePackage obj)
    {
        return new RolePackageDto()
        {
            Id = obj.Id,
            Role = DtoMapper.Convert(obj.Role),
            Package = DtoMapper.Convert(obj.Package),
            EntityVariant = obj.EntityVariant,
            HasAccess = obj.HasAccess,
            CanDelegate = obj.CanDelegate
        };
    }

    public static RoleDto Convert(Role obj)
    {
        return new RoleDto()
        {
            Id = obj.Id,
            Name = obj.Name,
            Description = obj.Description,
            Code = obj.Code,
            IsKeyRole = obj.IsKeyRole,
            Urn = obj.Urn,
            Provider = obj.Provider,
            LegacyRoleCode = null,
            LegacyUrn = null
        };
    }

    public static AreaGroupDto Convert(AreaGroup areaGroup)
    {
        return new AreaGroupDto()
        {
            Id = areaGroup.Id,
            Name = areaGroup.Name,
            Description = areaGroup.Description,
            Type = areaGroup.EntityType.Name,
            Urn = areaGroup.Urn,
            Areas = new List<AreaDto>()
        };
    }

    public static AreaGroupDto Convert(AreaGroup areaGroup, List<Area> areas)
    {
        return new AreaGroupDto()
        {
            Id = areaGroup.Id,
            Name = areaGroup.Name,
            Description = areaGroup.Description,
            Type = areaGroup.EntityType.Name,
            Urn = areaGroup.Urn,
            Areas = areas.Select(Convert).ToList()
        };
    }

    public static AreaDto Convert(Area area)
    {
        return new AreaDto()
        {
            Id = area.Id,
            Name = area.Name,
            Urn = area.Urn,
            Description = area.Description,
            Icon = area.IconUrl,
            Packages = new List<PackageDto>()
        };
    }

    public static AreaDto Convert(Area area, List<Package> packages)
    {
        return new AreaDto()
        {
            Id = area.Id,
            Name = area.Name,
            Urn = area.Urn,
            Description = area.Description,
            Icon = area.IconUrl,
            Packages = packages.Select(Convert).ToList()
        };
    }
}
