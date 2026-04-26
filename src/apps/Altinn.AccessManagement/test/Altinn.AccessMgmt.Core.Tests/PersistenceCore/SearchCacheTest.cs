using Altinn.AccessMgmt.Persistence.Core.Utilities.Search;
using Microsoft.Extensions.Caching.Memory;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

/// <summary>
/// Pure unit tests for <see cref="SearchCache{T}"/> — no database required.
/// </summary>
public class SearchCacheTest
{
    private static SearchCache<T> MakeCache<T>() =>
        new SearchCache<T>(new MemoryCache(new MemoryCacheOptions()));

    // ── GetData before SetData ────────────────────────────────────────────────

    [Fact]
    public void GetData_BeforeSetData_ReturnsNull()
    {
        var cache = MakeCache<string>();
        cache.GetData().Should().BeNull();
    }

    // ── SetData + GetData ────────────────────────────────────────────────────

    [Fact]
    public void GetData_AfterSetData_ReturnsStoredItems()
    {
        var cache = MakeCache<string>();
        var data = new List<string> { "alpha", "beta", "gamma" };

        cache.SetData(data, TimeSpan.FromMinutes(5));

        cache.GetData().Should().BeEquivalentTo(data);
    }

    [Fact]
    public void GetData_AfterSetData_ReturnsNewListEachCall()
    {
        var cache = MakeCache<int>();
        cache.SetData(new List<int> { 1, 2, 3 }, TimeSpan.FromMinutes(5));

        var first = cache.GetData();
        var second = cache.GetData();

        first.Should().NotBeSameAs(second);
        first.Should().BeEquivalentTo(second);
    }

    // ── value-type items ─────────────────────────────────────────────────────

    [Fact]
    public void GetData_IntList_RoundTripsCorrectly()
    {
        var cache = MakeCache<int>();
        cache.SetData(new List<int> { 10, 20, 30 }, TimeSpan.FromSeconds(30));

        cache.GetData().Should().Equal(10, 20, 30);
    }

    // ── empty list ───────────────────────────────────────────────────────────

    [Fact]
    public void GetData_EmptyList_ReturnsEmptyList()
    {
        var cache = MakeCache<string>();
        cache.SetData(new List<string>(), TimeSpan.FromMinutes(1));

        cache.GetData().Should().NotBeNull().And.BeEmpty();
    }

    // ── overwrite ────────────────────────────────────────────────────────────

    [Fact]
    public void SetData_CalledTwice_LatestDataWins()
    {
        var cache = MakeCache<string>();
        cache.SetData(new List<string> { "old" }, TimeSpan.FromMinutes(5));
        cache.SetData(new List<string> { "new-a", "new-b" }, TimeSpan.FromMinutes(5));

        cache.GetData().Should().BeEquivalentTo("new-a", "new-b");
    }
}
