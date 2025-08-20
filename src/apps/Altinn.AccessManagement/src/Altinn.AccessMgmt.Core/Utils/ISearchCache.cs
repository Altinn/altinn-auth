namespace Altinn.AccessMgmt.Core.Utils;

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
