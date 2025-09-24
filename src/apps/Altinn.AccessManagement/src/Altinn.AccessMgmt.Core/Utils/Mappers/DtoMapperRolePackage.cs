using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

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
            Area = Convert(area),
            Resources = resources.Select(Convert).ToList()
        };
    }

    public static RolePackageDto Convert(RolePackage obj)
    {
        return new RolePackageDto()
        {
            Id = obj.Id,
            Role = DtoMapper.Convert(obj.Role),
            Package = DtoMapper.Convert(obj.Package),
            EntityVariant = Convert(obj.EntityVariant),
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
            Provider = Convert(obj.Provider),
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

    public static EntityVariantDto Convert(EntityVariant entityVariant)
    {
        return new EntityVariantDto()
        {
            Id = entityVariant.Id,
            Name = entityVariant.Name,
            Description = entityVariant.Description,
            TypeId = entityVariant.TypeId,
            Type = Convert(entityVariant.Type)
        };
    }

    public static EntityTypeDto Convert(EntityType entityType) {
        return new EntityTypeDto()
        {
            Id = entityType.Id,
            ProviderId = entityType.ProviderId,
            Name = entityType.Name,
            Provider = Convert(entityType.Provider)
        };
    }

    public static ProviderDto Convert(Provider provider) {
        return new ProviderDto()
        {
            Id = provider.Id,
            Name = provider.Name,
            RefId = provider.RefId,
            LogoUrl = provider.LogoUrl,
            Code = provider.Code,
            TypeId = provider.TypeId,
            Type = Convert(provider.Type)
        };
    }

    public static ProviderTypeDto Convert(ProviderType entityType) {
        return new ProviderTypeDto()
        { 
            Id = entityType.Id, 
            Name = entityType.Name
        };
    }

    public static ResourceTypeDto Convert(ResourceType resourceType)
    {
        return new ResourceTypeDto
        {
            Id = resourceType.Id,
            Name = resourceType.Name,
        };
    }

    public static ResourceDto Convert(Resource resource) 
    {
        return new ResourceDto()
        {
            Id = resource.Id,
            Name = resource.Name,
            Description= resource.Description,
            TypeId = resource.TypeId,
            Type = Convert(resource.Type),
            ProviderId = resource.ProviderId,
            Provider = Convert(resource.Provider),
            RefId = resource.RefId
        };
    }
}
