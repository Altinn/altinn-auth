using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class PackageLookupService(AppDbContext db, IMemoryCache cache) : IPackageLookupService
{
    private const string CacheKey = "packageLookupIndex";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Package>> GetPackages(CancellationToken cancellationToken = default)
    {
        var index = await GetIndex(cancellationToken);
        return index.Packages;
    }

    /// <inheritdoc/>
    public async Task<Package> FindByUrn(string urn, CancellationToken cancellationToken = default)
    {
        var index = await GetIndex(cancellationToken);
        return index.FindByUrn(urn);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, Package>> FindByUrns(IEnumerable<string> urns, CancellationToken cancellationToken = default)
    {
        var index = await GetIndex(cancellationToken);
        var result = new Dictionary<string, Package>(StringComparer.Ordinal);

        foreach (var urn in urns.Where(u => !string.IsNullOrEmpty(u)).Distinct(StringComparer.Ordinal))
        {
            if (index.FindByUrn(urn) is { } package)
            {
                result[urn] = package;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Package> Find(string value, CancellationToken cancellationToken = default)
    {
        var index = await GetIndex(cancellationToken);
        return index.Find(value);
    }

    private async Task<PackageIndex> GetIndex(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out PackageIndex index))
        {
            return index;
        }

        var packages = await db.Packages
            .AsNoTracking()
            .Include(p => p.EntityType)
            .ToListAsync(cancellationToken);

        index = new PackageIndex(packages);
        cache.Set(CacheKey, index, CacheOptions);
        return index;
    }
}
