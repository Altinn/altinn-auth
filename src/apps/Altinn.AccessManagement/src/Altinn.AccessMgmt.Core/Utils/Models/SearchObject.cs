using Altinn.AccessMgmt.Core.Utils;

namespace Altinn.AccessMgmt.Core.Utils.Models;

/// <summary>
/// Represents the result of a fuzzy search operation.
/// </summary>
/// <typeparam name="T">The type of object being searched.</typeparam>
public class SearchObject<T>
{
    /// <summary>
    /// Gets or sets the object that matched the search criteria.
    /// </summary>
    public T Object { get; set; }

    /// <summary>
    /// Gets or sets the overall relevance score of the match.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the list of fields within the object where matches were found.
    /// </summary>
    public List<SearchField> Fields { get; set; } = new();
}
