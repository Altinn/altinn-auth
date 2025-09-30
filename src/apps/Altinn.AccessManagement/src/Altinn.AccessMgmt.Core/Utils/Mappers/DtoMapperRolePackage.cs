using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Maps EF entities to API DTOs.
/// </summary>
public partial class DtoMapper : IDtoMapper
{
    /// <summary>Convert Package to PackageDto.</summary>
    public static PackageDto? Convert(Package? obj) =>
        obj is null ? null : new PackageDto
        {
            Id = obj.Id,
            Name = obj.Name,
            Urn = obj.Urn,
            Description = obj.Description,
            IsDelegable = obj.IsDelegable,
            IsAssignable = obj.IsAssignable
        };

    /// <summary>Convert Package (with Area and Resources) to PackageDto.</summary>
    public static PackageDto? Convert(Package? obj, Area? area, IEnumerable<Resource>? resources) =>
        obj is null ? null : new PackageDto
        {
            Id = obj.Id,
            Name = obj.Name,
            Urn = obj.Urn,
            Description = obj.Description,
            IsDelegable = obj.IsDelegable,
            IsAssignable = obj.IsAssignable,
            Area = Convert(area),
            Resources = resources?.Select(r => Convert(r)!).ToList() ?? new()
        };

    /// <summary>Convert RolePackage to RolePackageDto.</summary>
    public static RolePackageDto? Convert(RolePackage? obj) =>
        obj is null ? null : new RolePackageDto
        {
            Id = obj.Id,
            Role = Convert(obj.Role),
            Package = Convert(obj.Package),
            EntityVariant = Convert(obj.EntityVariant),
            HasAccess = obj.HasAccess,
            CanDelegate = obj.CanDelegate
        };

    /// <summary>Convert Role to RoleDto.</summary>
    public static RoleDto? Convert(Role? obj) =>
        obj is null ? null : new RoleDto
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

    /// <summary>Convert AreaGroup to AreaGroupDto.</summary>
    public static AreaGroupDto? Convert(AreaGroup? areaGroup) =>
        areaGroup is null ? null : new AreaGroupDto
        {
            Id = areaGroup.Id,
            Name = areaGroup.Name,
            Description = areaGroup.Description,
            Type = areaGroup.EntityType.Name,
            Urn = areaGroup.Urn,
            Areas = new List<AreaDto>()
        };

    /// <summary>Convert AreaGroup with Areas to AreaGroupDto.</summary>
    public static AreaGroupDto? Convert(AreaGroup? areaGroup, List<Area>? areas) =>
        areaGroup is null ? null : new AreaGroupDto
        {
            Id = areaGroup.Id,
            Name = areaGroup.Name,
            Description = areaGroup.Description,
            Type = areaGroup.EntityType.Name,
            Urn = areaGroup.Urn,
            Areas = areas?.Select(a => Convert(a)!).ToList() ?? new()
        };

    /// <summary>Convert Area to AreaDto.</summary>
    public static AreaDto? Convert(Area? area) =>
        area is null ? null : new AreaDto
        {
            Id = area.Id,
            Name = area.Name,
            Urn = area.Urn,
            Description = area.Description,
            Icon = area.IconUrl,
            Packages = new List<PackageDto>()
        };

    /// <summary>Convert Area with Packages to AreaDto.</summary>
    public static AreaDto? Convert(Area? area, List<Package>? packages) =>
        area is null ? null : new AreaDto
        {
            Id = area.Id,
            Name = area.Name,
            Urn = area.Urn,
            Description = area.Description,
            Icon = area.IconUrl,
            Packages = packages?.Select(p => Convert(p)!).ToList() ?? new()
        };

    /// <summary>Convert EntityVariant to EntityVariantDto.</summary>
    public static EntityVariantDto? Convert(EntityVariant? entityVariant) =>
        entityVariant is null ? null : new EntityVariantDto
        {
            Id = entityVariant.Id,
            Name = entityVariant.Name,
            Description = entityVariant.Description,
            TypeId = entityVariant.TypeId,
            Type = Convert(entityVariant.Type)
        };

    /// <summary>Convert EntityType to EntityTypeDto.</summary>
    public static EntityTypeDto? Convert(EntityType? entityType) =>
        entityType is null ? null : new EntityTypeDto
        {
            Id = entityType.Id,
            ProviderId = entityType.ProviderId,
            Name = entityType.Name,
            Provider = Convert(entityType.Provider)
        };

    /// <summary>Convert Provider to ProviderDto.</summary>
    public static ProviderDto? Convert(Provider? provider) =>
        provider is null ? null : new ProviderDto
        {
            Id = provider.Id,
            Name = provider.Name,
            RefId = provider.RefId,
            LogoUrl = provider.LogoUrl,
            Code = provider.Code,
            TypeId = provider.TypeId,
            Type = Convert(provider.Type)
        };

    /// <summary>Convert ProviderType to ProviderTypeDto.</summary>
    public static ProviderTypeDto? Convert(ProviderType? providerType) =>
        providerType is null ? null : new ProviderTypeDto
        {
            Id = providerType.Id,
            Name = providerType.Name
        };

    /// <summary>Convert ResourceType to ResourceTypeDto.</summary>
    public static ResourceTypeDto? Convert(ResourceType? resourceType) =>
        resourceType is null ? null : new ResourceTypeDto
        {
            Id = resourceType.Id,
            Name = resourceType.Name
        };

    /// <summary>Convert Resource to ResourceDto.</summary>
    public static ResourceDto? Convert(Resource? resource) =>
        resource is null ? null : new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            TypeId = resource.TypeId,
            Type = Convert(resource.Type),
            ProviderId = resource.ProviderId,
            Provider = Convert(resource.Provider),
            RefId = resource.RefId
        };
}
