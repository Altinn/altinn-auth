using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Services;

/// <summary>
/// The provider and resource filters that <see cref="AuthorizedPartiesServiceEf"/> caches are rebuilt with their
/// case-insensitive comparer on a hit: the cache serializer keeps dictionary entries but not comparers, which made
/// the role, resource and package lookups case-sensitive after the first call.
/// </summary>
[UnitTest]
public class AuthorizedPartiesServiceEfFilterCacheTest
{
    [Fact]
    public async Task ApplyCachedFilters_AfterARoundTripThroughTheCache_LookupsIgnoreCase()
    {
        HybridCache cache = new ServiceCollection().AddHybridCache().Services.BuildServiceProvider().GetRequiredService<HybridCache>();
        Guid packageId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var stored = new AuthorizedPartiesServiceEf.ProviderResourceFilters(["app_ttd_test"], [packageId], ["dagl"]);
        await cache.SetAsync("filters", stored, cancellationToken: TestContext.Current.CancellationToken);
        var cached = await cache.GetOrCreateAsync<AuthorizedPartiesServiceEf.ProviderResourceFilters>("filters", _ => throw new InvalidOperationException("expected a cache hit"), cancellationToken: TestContext.Current.CancellationToken);

        var filter = new AuthorizedPartiesFilters();
        AuthorizedPartiesServiceEf.ApplyCachedFilters(filter, cached);

        Assert.True(filter.ResourceFilter.ContainsKey("APP_TTD_TEST"));
        Assert.True(filter.RoleFilter.ContainsKey("DAGL"));
        Assert.True(filter.PackageFilter.ContainsKey(packageId));
    }
}
