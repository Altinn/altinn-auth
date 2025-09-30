using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class PackageService(
    AppDbContext db
    ) : IPackageService
{

    /// <inheritdoc/>
    public async Task<IEnumerable<SearchObject<PackageDto>>> Search(string term, bool searchInResources = false, CancellationToken cancellationToken = default)
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
            .Add(pkg => pkg.Area.Name, 1.5, FuzzynessLevel.Medium);
            //.Add(pkg => pkg.Area.Group.Name, 1.3, FuzzynessLevel.Medium);

        if (searchInResources)
        {
            builder
                .AddCollection(pkg => pkg.Resources, r => r.Name, 1.2, FuzzynessLevel.High, detailed);
            //// .AddCollection(pkg => pkg.Resources, r => r.Description, 0.7, FuzzynessLevel.Low, detailed);
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

    private async Task<List<PackageDto>> GetSearchData(CancellationToken cancellationToken = default)
    {
        var areas = await db.Areas.AsNoTracking().ToListAsync(cancellationToken);
        var packages = await db.Packages.AsNoTracking().ToListAsync(cancellationToken);

        var result = new List<PackageDto>();
        foreach (var package in packages)
        {
            result.Add(DtoMapper.Convert(package, areas.First(t => t.Id == package.AreaId), await db.PackageResources.AsNoTracking().Where(t => t.PackageId == package.Id).Include(t => t.Resource).Select(t => t.Resource).ToListAsync(cancellationToken)));
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

        var packages = await db.Packages.AsNoTracking().Where(t => t.Urn.EndsWith(urnValue)).Include(t => t.Area).ToListAsync(cancellationToken);
        if (packages == null || packages.Count() != 1)
        {
            return null;
        }

        var package = packages.First();

        var resources = await db.PackageResources.AsNoTracking().Where(t => t.PackageId == package.Id).Include(t => t.Resource).Select(t => t.Resource).ToListAsync(cancellationToken);

        return DtoMapper.Convert(package, package.Area, resources);
    }

    /// <inheritdoc/>
    public async Task<PackageDto> GetPackage(Guid id, CancellationToken cancellationToken = default)
    {
        var package = await db.Packages.AsNoTracking().Include(t => t.Area).SingleAsync(t => t.Id == id, cancellationToken);

        if (package == null)
        {
            return null;
        }

        var resources = await db.PackageResources.AsNoTracking().Where(t => t.PackageId == package.Id).Include(t => t.Resource).Select(t => t.Resource).ToListAsync(cancellationToken);

        return DtoMapper.Convert(package, package.Area, resources);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PackageDto>> GetPackagesByArea(Guid areaId, CancellationToken cancellationToken)
    {
        var packages = await db.Packages.AsNoTracking().Include(t => t.Area).Include(t => t.Provider).Include(t => t.EntityType).Where(t => t.AreaId == areaId).Include(t => t.Area).ToListAsync(cancellationToken);

        var result = new List<PackageDto>();
        foreach (var package in packages)
        {
            result.Add(DtoMapper.Convert(package, package.Area, await db.PackageResources.AsNoTracking().Where(t => t.PackageId == package.Id).Include(t => t.Resource).Select(t => t.Resource).ToListAsync(cancellationToken)));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaGroupDto>> GetHierarchy(CancellationToken cancellationToken = default)
    {
        var groups = await db.AreaGroups.AsNoTracking().Include(t => t.EntityType).ToListAsync(cancellationToken);
        var areas = await db.Areas.AsNoTracking().Include(t => t.Group).ToListAsync(cancellationToken);
        var packages = await db.Packages.AsNoTracking().Include(t => t.Area).Include(t => t.Provider).Include(t => t.EntityType).ToListAsync(cancellationToken);

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
        return (await db.AreaGroups.AsNoTracking().Include(t => t.EntityType).ToListAsync(cancellationToken)).Select(DtoMapper.Convert);
    }

    /// <inheritdoc/>
    public async Task<AreaGroupDto> GetAreaGroup(Guid id, CancellationToken cancellationToken = default)
    {
        return DtoMapper.Convert(await db.AreaGroups.AsNoTracking().Include(t => t.EntityType).SingleAsync(t => t.Id == id, cancellationToken));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaDto>> GetAreas(Guid groupId, CancellationToken cancellationToken = default)
    {
        return (await db.Areas.AsNoTracking().ToListAsync(cancellationToken)).Select(DtoMapper.Convert);
    }

    /// <inheritdoc/>
    public async Task<AreaDto> GetArea(Guid id, CancellationToken cancellationToken = default)
    {
        return DtoMapper.Convert(await db.Areas.AsNoTracking().SingleAsync(t => t.Id == id, cancellationToken));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ResourceDto>> GetPackageResources(Guid packageId, CancellationToken cancellationToken = default)
    {
        return (await db.PackageResources.AsNoTracking().Where(t => t.PackageId == packageId).Select(t => t.Resource).ToListAsync()).Select(DtoMapper.Convert);
    }
}
