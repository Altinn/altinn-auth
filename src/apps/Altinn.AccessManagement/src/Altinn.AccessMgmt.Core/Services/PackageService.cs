using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class PackageService : IPackageService
{
    public AppDbContext DbContext { get; set; }

    public PackageService(AppDbContext appDbContext)
    {
        DbContext = appDbContext;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SearchObject<PackageDto>>> Search(string term, List<string> resourceProviderCodes = null, bool searchInResources = false, Guid? typeId = null, CancellationToken cancellationToken = default)
    {
        var data = await GetSearchData(resourceProviderCodes: resourceProviderCodes, typeId: typeId);

        if (string.IsNullOrEmpty(term))
        {
            return data.Select(t => new SearchObject<PackageDto>() { Object = t, Score = 0, Fields = [] });
        }

        bool detailed = false;

        var builder = new SearchPropertyBuilder<PackageDto>()
            .Add(pkg => pkg.Name, 2.0, FuzzynessLevel.High)
            .Add(pkg => pkg.Description, 0.8, FuzzynessLevel.Low)
            .Add(pkg => pkg.Area.Name, 1.5, FuzzynessLevel.Medium);
            ////.Add(pkg => pkg.Area.Group.Name, 1.3, FuzzynessLevel.Medium);

        if (searchInResources)
        {
            builder
                .AddCollection(pkg => pkg.Resources, r => r.Name, 1.2, FuzzynessLevel.High, detailed);
                ////.AddCollection(pkg => pkg.Resources, r => r.Description, 0.7, FuzzynessLevel.Low, detailed);
        }

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

    private async Task<List<PackageDto>> GetSearchData(List<string> resourceProviderCodes = null, Guid? typeId = null, CancellationToken cancellationToken = default)
    {
        bool filterResourceProviders = resourceProviderCodes != null && resourceProviderCodes.Any();

        var areas = await DbContext.Areas.AsNoTracking().ToListAsync(cancellationToken);
        var packages = await DbContext.Packages.AsNoTracking().Include(t => t.EntityType).WhereIf(typeId.HasValue, t => t.EntityTypeId == typeId.Value).ToListAsync(cancellationToken);

        var result = new List<PackageDto>();

        var packageResources = await DbContext.PackageResources.AsNoTracking()
            .Include(t => t.Resource)
            .Include(t => t.Resource).ThenInclude(t => t.Provider)
            .Include(t => t.Resource).ThenInclude(t => t.Type)
            .WhereIf(filterResourceProviders, t => resourceProviderCodes.Any(code => EF.Functions.ILike(t.Resource.Provider.Code, "%" + code + "%")))
            .ToListAsync(cancellationToken);

        foreach (var package in packages)
        {
            // Skip package if the package does not have a valid filtered resource
            if (filterResourceProviders == true && packageResources.Any(t => t.PackageId == package.Id) == false)
            {
                continue;
            }

            result.Add(DtoMapper.Convert(package, areas.First(t => t.Id == package.AreaId), packageResources.Where(t => t.PackageId == package.Id).Select(t => t.Resource)));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<PackageDto> GetPackageByUrnValue(string urnValue, CancellationToken cancellationToken = default)
    {
        if (!urnValue.StartsWith("urn:") && !urnValue.StartsWith(':'))
        {
            urnValue = ":" + urnValue;
        }

        var packages = await DbContext.Packages.AsNoTracking().Where(t => t.Urn.ToLower().EndsWith(urnValue.ToLower())).Include(t => t.Area).Include(t => t.EntityType).ToListAsync(cancellationToken);
        if (packages == null || packages.Count() != 1)
        {
            return null;
        }

        var package = packages.First();

        var resources = await DbContext.PackageResources.AsNoTracking().Where(t => t.PackageId == package.Id).Include(t => t.Resource).Select(t => t.Resource).ToListAsync(cancellationToken);

        return DtoMapper.Convert(package, package.Area, resources);
    }

    /// <inheritdoc/>
    public async Task<PackageDto> GetPackage(Guid id, CancellationToken cancellationToken = default)
    {
        var package = await DbContext.Packages.AsNoTracking().Include(t => t.Area).Include(t => t.EntityType).SingleAsync(t => t.Id == id, cancellationToken);

        if (package == null)
        {
            return null;
        }

        var resources = await GetDbPackageResources(id, cancellationToken);

        return DtoMapper.Convert(package, package.Area, resources);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PackageDto>> GetPackagesByArea(Guid areaId, CancellationToken cancellationToken)
    {
        var packages = await DbContext.Packages.AsNoTracking().Include(t => t.Area).Include(t => t.Provider).Include(t => t.EntityType).Where(t => t.AreaId == areaId).Include(t => t.Area).ToListAsync(cancellationToken);

        var result = new List<PackageDto>();
        foreach (var package in packages)
        {
            result.Add(DtoMapper.Convert(package, package.Area, await DbContext.PackageResources.AsNoTracking().Where(t => t.PackageId == package.Id).Include(t => t.Resource).Select(t => t.Resource).ToListAsync(cancellationToken)));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaGroupDto>> GetHierarchy(CancellationToken cancellationToken = default)
    {
        var groups = await DbContext.AreaGroups.AsNoTracking().Include(t => t.EntityType).ToListAsync(cancellationToken);
        var areas = await DbContext.Areas.AsNoTracking().Include(t => t.Group).ToListAsync(cancellationToken);
        var packages = await DbContext.Packages.AsNoTracking().Include(t => t.Area).Include(t => t.Provider).Include(t => t.EntityType).ToListAsync(cancellationToken);

        var result = groups.Select(DtoMapper.Convert).ToList();
        foreach (var grp in result)
        {
            grp.Areas = areas.Where(t => t.GroupId == grp.Id).Select(DtoMapper.Convert).ToList();
            foreach (var area in grp.Areas)
            {
                area.Packages = packages.Where(t => t.AreaId == area.Id).Select(DtoMapper.Convert).ToList();
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaGroupDto>> GetAreaGroups(CancellationToken cancellationToken = default)
    {
        return (await DbContext.AreaGroups.AsNoTracking().Include(t => t.EntityType).ToListAsync(cancellationToken)).Select(DtoMapper.Convert);
    }

    /// <inheritdoc/>
    public async Task<AreaGroupDto> GetAreaGroup(Guid id, CancellationToken cancellationToken = default)
    {
        return DtoMapper.Convert(await DbContext.AreaGroups.AsNoTracking().Include(t => t.EntityType).SingleAsync(t => t.Id == id, cancellationToken));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaDto>> GetAreas(Guid groupId, CancellationToken cancellationToken = default)
    {
        return (await DbContext.Areas.AsNoTracking().ToListAsync(cancellationToken)).Select(DtoMapper.Convert);
    }

    /// <inheritdoc/>
    public async Task<AreaDto> GetArea(Guid id, CancellationToken cancellationToken = default)
    {
        return DtoMapper.Convert(await DbContext.Areas.AsNoTracking().SingleAsync(t => t.Id == id, cancellationToken));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ResourceDto>> GetPackageResources(Guid packageId, CancellationToken cancellationToken = default)
    {
        return (await GetDbPackageResources(packageId, cancellationToken)).Select(DtoMapper.Convert);
    }

    private async Task<List<Resource>> GetDbPackageResources(Guid packageId, CancellationToken cancellationToken = default)
    {
        return await DbContext.PackageResources.AsNoTracking()
            .Where(t => t.PackageId == packageId)
            .Include(t => t.Resource)
            .Include(t => t.Resource).ThenInclude(t => t.Provider)
            .Include(t => t.Resource).ThenInclude(t => t.Type)
            .Select(t => t.Resource).ToListAsync(cancellationToken);
    }
}
