using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Api.Metadata.Translation;

/// <summary>
/// Extension methods for deep translation of DTOs with nested objects
/// </summary>
public static class DeepTranslationExtensions
{
    /// <summary>
    /// Translates a PackageDto with all nested objects (Area, Resources, Type, etc.)
    /// </summary>
    public static async ValueTask<PackageDto> TranslateDeepAsync(
        this PackageDto package,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (package == null)
        {
            return package;
        }

        // Translate the package itself
        package = await translationService.TranslateAsync(package, languageCode, allowPartial);

        // Translate nested Area
        if (package.Area != null)
        {
            package.Area = await package.Area.TranslateDeepAsync(translationService, languageCode, allowPartial);
        }

        // Translate nested Type
        if (package.Type != null)
        {
            package.Type = await package.Type.TranslateDeepAsync(translationService, languageCode, allowPartial);
        }

        // Translate Resources collection
        if (package.Resources != null)
        {
            package.Resources = await package.Resources.TranslateDeepAsync(translationService, languageCode, allowPartial);
        }

        return package;
    }

    /// <summary>
    /// Translates a collection of PackageDtos with all nested objects
    /// </summary>
    public static async ValueTask<IEnumerable<PackageDto>> TranslateDeepAsync(
        this IEnumerable<PackageDto> packages,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (packages == null)
        {
            return packages;
        }

        var result = new List<PackageDto>();
        foreach (var package in packages)
        {
            var translated = await package.TranslateDeepAsync(translationService, languageCode, allowPartial);
            result.Add(translated);
        }
        
        return result;
    }

    /// <summary>
    /// Translates an AreaDto with all nested objects (Group, Packages)
    /// </summary>
    public static async ValueTask<AreaDto> TranslateDeepAsync(
        this AreaDto area,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (area == null)
        {
            return area;
        }

        // Translate the area itself
        area = await translationService.TranslateAsync(area, languageCode, allowPartial);

        // Translate nested Group
        if (area.Group != null)
        {
            area.Group = await area.Group.TranslateDeepAsync(translationService, languageCode, allowPartial);
        }

        // Translate nested Packages (if present, avoiding circular translation)
        if (area.Packages != null && area.Packages.Any())
        {
            var translatedPackages = new List<PackageDto>();
            foreach (var package in area.Packages)
            {
                // Translate package but avoid re-translating the area (circular reference)
                var translatedPackage = await translationService.TranslateAsync(package, languageCode, allowPartial);
                
                // Translate nested Type
                if (translatedPackage.Type != null)
                {
                    translatedPackage.Type = await translatedPackage.Type.TranslateDeepAsync(translationService, languageCode, allowPartial);
                }

                // Translate Resources
                if (translatedPackage.Resources != null)
                {
                    translatedPackage.Resources = await translatedPackage.Resources.TranslateDeepAsync(translationService, languageCode, allowPartial);
                }

                translatedPackages.Add(translatedPackage);
            }

            area.Packages = translatedPackages;
        }

        return area;
    }

    /// <summary>
    /// Translates a collection of AreaDtos with all nested objects
    /// </summary>
    public static async ValueTask<IEnumerable<AreaDto>> TranslateDeepAsync(
        this IEnumerable<AreaDto> areas,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (areas == null)
        {
            return areas;
        }

        var result = new List<AreaDto>();
        foreach (var area in areas)
        {
            var translated = await area.TranslateDeepAsync(translationService, languageCode, allowPartial);
            result.Add(translated);
        }
        
        return result;
    }

    /// <summary>
    /// Translates an AreaGroupDto with all nested objects (Areas)
    /// </summary>
    public static async ValueTask<AreaGroupDto> TranslateDeepAsync(
        this AreaGroupDto areaGroup,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (areaGroup == null)
        {
            return areaGroup;
        }

        // Translate the area group itself
        areaGroup = await translationService.TranslateAsync(areaGroup, languageCode, allowPartial);

        // Translate nested Areas collection
        if (areaGroup.Areas != null)
        {
            areaGroup.Areas = (await areaGroup.Areas.TranslateDeepAsync(translationService, languageCode, allowPartial)).ToList();
        }

        return areaGroup;
    }

    /// <summary>
    /// Translates a collection of AreaGroupDtos with all nested objects
    /// </summary>
    public static async ValueTask<IEnumerable<AreaGroupDto>> TranslateDeepAsync(
        this IEnumerable<AreaGroupDto> areaGroups,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (areaGroups == null)
        {
            return areaGroups;
        }

        var result = new List<AreaGroupDto>();
        foreach (var areaGroup in areaGroups)
        {
            var translated = await areaGroup.TranslateDeepAsync(translationService, languageCode, allowPartial);
            result.Add(translated);
        }
        
        return result;
    }

    /// <summary>
    /// Translates a ResourceDto with all nested objects (Provider, Type)
    /// </summary>
    public static async ValueTask<ResourceDto> TranslateDeepAsync(
        this ResourceDto resource,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (resource == null)
        {
            return resource;
        }

        // Translate the resource itself
        resource = await translationService.TranslateAsync(resource, languageCode, allowPartial);

        // Translate nested Provider
        if (resource.Provider != null)
        {
            resource.Provider = await resource.Provider.TranslateDeepAsync(translationService, languageCode, allowPartial);
        }

        // Translate nested Type
        if (resource.Type != null)
        {
            resource.Type = await translationService.TranslateAsync(resource.Type, languageCode, allowPartial);
        }

        return resource;
    }

    /// <summary>
    /// Translates a collection of ResourceDtos with all nested objects
    /// </summary>
    public static async ValueTask<IEnumerable<ResourceDto>> TranslateDeepAsync(
        this IEnumerable<ResourceDto> resources,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (resources == null)
        {
            return resources;
        }

        var result = new List<ResourceDto>();
        foreach (var resource in resources)
        {
            var translated = await resource.TranslateDeepAsync(translationService, languageCode, allowPartial);
            result.Add(translated);
        }
        
        return result;
    }

    /// <summary>
    /// Translates a ProviderDto with all nested objects (Type)
    /// </summary>
    public static async ValueTask<ProviderDto> TranslateDeepAsync(
        this ProviderDto provider,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (provider == null)
        {
            return provider;
        }

        // Translate the provider itself
        provider = await translationService.TranslateAsync(provider, languageCode, allowPartial);

        // Translate nested Type
        if (provider.Type != null)
        {
            provider.Type = await translationService.TranslateAsync(provider.Type, languageCode, allowPartial);
        }

        return provider;
    }

    /// <summary>
    /// Translates a TypeDto with all nested objects (Provider)
    /// </summary>
    public static async ValueTask<TypeDto> TranslateDeepAsync(
        this TypeDto type,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (type == null)
        {
            return type;
        }

        // Translate the type itself
        type = await translationService.TranslateAsync(type, languageCode, allowPartial);

        // Translate nested Provider
        if (type.Provider != null)
        {
            type.Provider = await type.Provider.TranslateDeepAsync(translationService, languageCode, allowPartial);
        }

        return type;
    }

    /// <summary>
    /// Translates a RoleDto with all nested objects (Provider)
    /// </summary>
    public static async ValueTask<RoleDto> TranslateDeepAsync(
        this RoleDto role,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (role == null)
        {
            return role;
        }

        // Translate the role itself
        role = await translationService.TranslateAsync(role, languageCode, allowPartial);

        // Translate nested Provider
        if (role.Provider != null)
        {
            role.Provider = await role.Provider.TranslateDeepAsync(translationService, languageCode, allowPartial);
        }

        return role;
    }

    /// <summary>
    /// Translates a collection of RoleDtos with all nested objects
    /// </summary>
    public static async ValueTask<IEnumerable<RoleDto>> TranslateDeepAsync(
        this IEnumerable<RoleDto> roles,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (roles == null)
        {
            return roles;
        }

        var result = new List<RoleDto>();
        foreach (var role in roles)
        {
            var translated = await role.TranslateDeepAsync(translationService, languageCode, allowPartial);
            result.Add(translated);
        }
        
        return result;
    }
}
