#nullable enable
using Microsoft.Extensions.Caching.Hybrid;

namespace Altinn.AccessManagement.Core.Helpers.Extensions;

/// <summary>
/// Two cache patterns <see cref="HybridCache"/> has no method for: a lookup that never calls a data source,
/// and a fetch that caches only a value, so a missing item is looked up again on the next call.
/// </summary>
internal static class HybridCacheExtensions
{
    private static readonly HybridCacheEntryOptions LookupOnly = new() { Flags = HybridCacheEntryFlags.DisableUnderlyingData };

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or <see langword="null"/> when there is none. No data source is called.
    /// </summary>
    public static ValueTask<T?> GetAsync<T>(this HybridCache cache, string key, CancellationToken cancellationToken)
        where T : class
        => cache.GetOrCreateAsync<T?>(key, static _ => default, LookupOnly, tags: null, cancellationToken);

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or calls <paramref name="fetch"/> and caches its result when it is not
    /// <see langword="null"/>. Unlike <c>GetOrCreateAsync</c>, a <see langword="null"/> result is not cached and concurrent
    /// misses each call <paramref name="fetch"/>, which is how the <c>IMemoryCache</c> code behaved.
    /// </summary>
    public static async ValueTask<T?> GetOrFetchAsync<T>(this HybridCache cache, string key, Func<CancellationToken, Task<T?>> fetch, HybridCacheEntryOptions options, IEnumerable<string> tags, CancellationToken cancellationToken)
        where T : class
    {
        T? cached = await cache.GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        T? value = await fetch(cancellationToken);
        if (value is not null)
        {
            await cache.SetAsync(key, value, options, tags, cancellationToken);
        }

        return value;
    }
}
