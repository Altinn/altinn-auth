using Microsoft.Extensions.Caching.Memory;

namespace Altinn.AccessMgmt.Persistence.Core.Utilities.Search;

/// <inheritdoc/>
public class SearchCache<T> : ISearchCache<T>
{
    private readonly IMemoryCache _cache;
    private readonly string _cacheKey;

    /// <summary>
    /// Default construtor for Search cache
    /// </summary>
    /// <param name="cache">IMemoryCache</param>
    public SearchCache(IMemoryCache cache)
    {
        _cache = cache;
        _cacheKey = typeof(T).FullName ?? Guid.CreateVersion7().ToString();
    }

    /// <inheritdoc/>
    public void SetData(List<T> data, TimeSpan duration)
    {
        _cache.Set(_cacheKey, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        });
    }

    /// <inheritdoc/>
    public List<T> GetData()
    {
        return _cache.TryGetValue(_cacheKey, out List<T> data) ? [.. data] : null;
    }
}

/// <summary>
/// SearchCache
/// </summary>
/// <typeparam name="T">Type to be cached</typeparam>
public interface ISearchCache<T>
{
    /// <summary>
    /// Sets data
    /// </summary>
    /// <param name="data">Data</param>
    /// <param name="duration">Duration of cache</param>
    void SetData(List<T> data, TimeSpan duration);

    /// <summary>
    /// Get data
    /// </summary>
    List<T> GetData();
}
