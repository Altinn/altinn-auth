using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class PackageService : IPackageService
{
    public AppDbContext DbContext { get; set; }

    public ITranslationService TranslationService { get; set; }

    public PackageService(AppDbContext appDbContext, ITranslationService translationService)
    {
        DbContext = appDbContext;
        TranslationService = translationService;
    }

    private const StringComparison Ic = StringComparison.InvariantCultureIgnoreCase;

    private sealed record ScoringRule(
        string FieldName,
        Func<PackageDto, string> Field,
        Func<string, string, bool> Match,
        int Points);

    private static readonly ScoringRule[] PackageRules =
    [
        new("name.prefix",      p => p.Name,             (f, t) => f.StartsWith(t, Ic), 100),
        new("name",             p => p.Name,             (f, t) => f.Contains(t, Ic),    50),
        new("description",      p => p.Description,      (f, t) => f.Contains(t, Ic),    10),
        new("area.name.prefix", p => p.Area.Name,        (f, t) => f.StartsWith(t, Ic),  25),
        new("area.description", p => p.Area.Description, (f, t) => f.Contains(t, Ic),     5),
    ];

    private static SearchObject<PackageDto> ScorePackage(
    PackageDto package,
    string term,
    bool searchInResources)
    {
        var totalScore = 0;
        var fields = new List<SearchField>();

        foreach (var rule in PackageRules)
        {
            var value = rule.Field(package);
            if (rule.Match(value, term))
            {
                totalScore += rule.Points;
                fields.Add(new SearchField
                {
                    Field = rule.FieldName,
                    Value = value,
                    Score = rule.Points,
                });
            }
        }

        if (searchInResources)
        {
            foreach (var resource in package.Resources.Where(t => t.Name.Contains(term, Ic)))
            {                
                totalScore += 2;
                fields.Add(new SearchField
                {
                    Field = "resources.name",
                    Value = resource.Name,
                    Score = 2,
                });   
            }
        }

        return new SearchObject<PackageDto>
        {
            Object = package,
            Score = totalScore,
            Fields = fields,
        };
    }

    public async Task<IEnumerable<SearchObject<PackageDto>>> SimpleSearch(string term, List<string> resourceProviderCodes = null, bool searchInResources = false, Guid? typeId = null, string languageCode = "nob", bool allowPartialTranslation = true, CancellationToken cancellationToken = default)
    {
        var data = await GetSearchData(
        resourceProviderCodes: resourceProviderCodes,
        typeId: typeId,
        languageCode: languageCode,
        allowPartialTranslation: allowPartialTranslation,
        cancellationToken: cancellationToken
        );

        if (string.IsNullOrEmpty(term))
        {
            return data.Select(t => new SearchObject<PackageDto>() { Object = t, Score = 0, Fields = [] });
        }

        return data
            .Select(p => ScorePackage(p, term, searchInResources))
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SearchObject<PackageDto>>> FuzzySearch(string term, List<string> resourceProviderCodes = null, bool searchInResources = false, Guid? typeId = null, string languageCode = "nob", bool allowPartialTranslation = true, CancellationToken cancellationToken = default)
    {
        var data = await GetSearchData(resourceProviderCodes: resourceProviderCodes, typeId: typeId, languageCode: languageCode, allowPartialTranslation: allowPartialTranslation, cancellationToken: cancellationToken);

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

        var results = Utils.FuzzySearch.PerformFuzzySearch(data, term, builder);

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

    private async Task<List<PackageDto>> GetSearchData(List<string> resourceProviderCodes = null, Guid? typeId = null, string languageCode = "nbNO", bool allowPartialTranslation = false, CancellationToken cancellationToken = default)
    {
        bool filterResourceProviders = resourceProviderCodes != null && resourceProviderCodes.Any();

        var areas = await DbContext.Areas.AsNoTracking().ToListAsync(cancellationToken);
        var packages = await DbContext.Packages.AsNoTracking().Include(t => t.EntityType).WhereIf(typeId.HasValue && typeId.Value != Guid.Empty, t => t.EntityTypeId == typeId.Value).ToListAsync(cancellationToken);

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

        foreach (var t in result)
        {
            await t.TranslateDeepAsync(TranslationService, languageCode: languageCode, allowPartial: allowPartialTranslation);
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
        var package = await DbContext.Packages.AsNoTracking().Include(t => t.Area).Include(t => t.EntityType).SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (package == null)
        {
            return null;
        }

        var resources = await GetDbPackageResources(id, cancellationToken);

        return DtoMapper.Convert(package, package.Area, resources);
    }

    /// <inheritdoc/>
    public async Task<PackageDto> GetPackage(RequestReferenceDto reference, CancellationToken cancellationToken = default)
    {
        if (reference.Id == null && string.IsNullOrEmpty(reference.ReferenceId))
        {
            return null;
        }

        var package = await DbContext.Packages.AsNoTracking()
            .Include(t => t.Area)
            .Include(t => t.EntityType)
            .WhereIf(reference.Id.HasValue && reference.Id.Value != Guid.Empty, t => t.Id == reference.Id.Value)
            .WhereIf(!string.IsNullOrEmpty(reference.ReferenceId), t => t.Urn == reference.ReferenceId)
            .SingleAsync(cancellationToken);

        if (package == null)
        {
            return null;
        }

        var resources = await GetDbPackageResources(package.Id, cancellationToken);

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
        var group = await DbContext.AreaGroups.AsNoTracking().Include(t => t.EntityType).SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        return group == null ? null : DtoMapper.Convert(group);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AreaDto>> GetAreas(Guid groupId, CancellationToken cancellationToken = default)
    {
        return (await DbContext.Areas.AsNoTracking().Where(t => t.GroupId == groupId).ToListAsync(cancellationToken)).Select(DtoMapper.Convert);
    }

    /// <inheritdoc/>
    public async Task<AreaDto> GetArea(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await DbContext.Areas.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        return area == null ? null : DtoMapper.Convert(area);
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
