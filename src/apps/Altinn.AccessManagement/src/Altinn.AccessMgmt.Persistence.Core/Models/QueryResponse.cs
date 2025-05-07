using System.Collections;

namespace Altinn.AccessMgmt.Persistence.Core.Models;

/// <summary>
/// Response from db query
/// </summary>
/// <typeparam name="T"></typeparam>
public class QueryResponse<T> : IEnumerable<T>
{
    /// <summary>
    /// Rows converted to objects
    /// </summary>
    public IEnumerable<T> Data { get; set; }
    
    /// <summary>
    /// Page information
    /// </summary>
    public QueryPageInfo Page { get; set; }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => Data.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
