using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Utilities.Search;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;

/// <inheritdoc/>
public class PackageService(
    IPackageRepository packageRepository,
    IAreaGroupRepository areaGroupRepository,
    IAreaRepository areaRepository,
    IPackageResourceRepository packageResourceRepository,
    IResourceRepository resourceRepository,
    ISearchCache<PackageDto> searchPackageCache
    ) : IPackageService
{
    private readonly IPackageRepository packageRepository = packageRepository;
    private readonly IAreaGroupRepository areaGroupRepository = areaGroupRepository;
    private readonly IAreaRepository areaRepository = areaRepository;
    private readonly IPackageResourceRepository packageResourceRepository = packageResourceRepository;
    private readonly IResourceRepository resourceRepository = resourceRepository;
    private readonly ISearchCache<PackageDto> searchPackageCache = searchPackageCache;

    /// <inheritdoc/>
    public async Task<IEnumerable<SearchObject<PackageDto>>> Search(string term)
    {
        var data = await GetSearchData();

        if (string.IsNullOrEmpty(term))
        {
            return data.Select(t => new SearchObject<PackageDto>() { Object = t, Score = 0, Fields = [] });
        }

        bool detailed = false;

        var builder = new SearchPropertyBuilder<PackageDto>()
            .Add(pkg => pkg.Name, 2.0, FuzzynessLevel.High)
            .Add(pkg => pkg.Description, 0.8, FuzzynessLevel.Low)
            .Add(pkg => pkg.Area.Name, 1.5, FuzzynessLevel.Medium)
            .Add(pkg => pkg.Area.Group.Name, 1.3, FuzzynessLevel.Medium)
            .AddCollection(pkg => pkg.Resources, r => r.Name, 1.2, FuzzynessLevel.High, detailed)
            .AddCollection(pkg => pkg.Resources, r => r.Description, 0.7, FuzzynessLevel.Low, detailed);

        var results = FuzzySearch.PerformFuzzySearch(data, term, builder);

        foreach (var res in results.OrderByDescending(t => t.Score))
        {
            Console.WriteLine($"ID: {res.Object.Id}, Name: {res.Object.Name}, Score: {res.Score}");
            foreach (var f in res.Fields.OrderByDescending(t => t.Score))
            {
                if (f.Words.Count > 0)
                {
                    Console.WriteLine($"{f.Field}, Score:{f.Score} , Matches: " + string.Join(" ", f.Words.Select(h => h.IsMatch ? $"[{h.Content}({h.Score})]" : h.Content)));
                }
            }
        }

        return results.OrderByDescending(t => t.Score).ToList();
    }

    private async Task<List<PackageDto>> GetSearchData()
    {
        var cache = searchPackageCache.GetData();
        if (cache != null)
        {
            return cache;
        }

        var areas = await areaRepository.GetExtended();
        var packages = await packageRepository.GetExtended();

        var result = new List<PackageDto>();
        foreach (var package in packages)
        {
            result.Add(new PackageDto(package)
            {
                Area = areas.First(t => t.Id == package.AreaId),
                Resources = await packageResourceRepository.GetB(package.Id)
            });
        }

        searchPackageCache.SetData(result, TimeSpan.FromMinutes(5));

        return result;
    }

    /// <inheritdoc/>
    public async Task<PackageDto> GetPackageByUrnValue(string urnValue)
    {
        var filter = packageRepository.CreateFilterBuilder();
        filter.Add(t => t.Urn, urnValue, FilterComparer.EndsWith);
        var packages = await packageRepository.GetExtended(filter);
        if (packages == null || packages.Count() != 1)
        {
            return null;
        }

        var package = packages.First();

        var area = await areaRepository.GetExtended(package.AreaId);
        var resources = await packageResourceRepository.GetB(package.Id);

        return new PackageDto(package)
        {
            Area = area,
            Resources = resources
        };
    }

    /// <inheritdoc/>
    public async Task<PackageDto> GetPackage(Guid id)
    {
        var package = await packageRepository.GetExtended(id);
        var area = await areaRepository.GetExtended(package.AreaId);
        var resources = await packageResourceRepository.GetB(package.Id);

        return new PackageDto(package)
        {
            Area = area,
            Resources = resources
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PackageDto>> GetPackagesByArea(Guid areaId)
    {
        var packages = await packageRepository.GetExtended(t => t.AreaId, areaId);
        var area = await areaRepository.GetExtended(areaId);

        var result = new List<PackageDto>();
        foreach (var package in packages)
        {
            result.Add(new PackageDto(package)
            {
                Area = area,
                Resources = await packageResourceRepository.GetB(package.Id)
            });
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaGroupDto>> GetHierarchy()
    {
        var groups = await areaGroupRepository.GetExtended();
        var areas = await areaRepository.GetExtended();
        var packages = await packageRepository.GetExtended();

        var result = new List<AreaGroupDto>();
        foreach (var package in packages)
        {
            var grpId = package.Area.GroupId;
            var areaId = package.Area.Id;

            if (result.Count(t => t.Id == grpId) == 0)
            {
                result.Add(ConvertToDto(groups.First(t => t.Id == grpId)));
            }

            var grp = result.First(t => t.Id == grpId);
            if (grp.Areas == null)
            {
                grp.Areas = new List<AreaDto>();
            }

            if (grp.Areas.Count(t => t.Id == areaId) == 0)
            {
                grp.Areas.Add(ConvertToDto(areas.First(t => t.Id == areaId)));
            }
            else
            {
                var area = grp.Areas.First(t => t.Id == areaId);
                area.Packages.Add(new PackageDto(package));
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExtAreaGroup>> GetAreaGroups()
    {
        return await areaGroupRepository.GetExtended();
    }

    /// <inheritdoc/>
    public async Task<ExtAreaGroup> GetAreaGroup(Guid id)
    {
        return await areaGroupRepository.GetExtended(id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Area>> GetAreas(Guid groupId)
    {
        return await areaRepository.Get();
    }

    /// <inheritdoc/>
    public async Task<Area> GetArea(Guid id)
    {
        return await areaRepository.Get(id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Resource>> GetPackageResources(Guid packageId)
    {
        return await packageResourceRepository.GetB(packageId);
    }

    #region Converters
    private AreaGroupDto ConvertToDto(ExtAreaGroup areaGroup)
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

    private AreaDto ConvertToDto(ExtArea area)
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
    #endregion
}
