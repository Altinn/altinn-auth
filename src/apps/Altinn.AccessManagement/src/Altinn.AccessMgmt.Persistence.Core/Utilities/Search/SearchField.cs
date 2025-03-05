namespace Altinn.AccessMgmt.Persistence.Core.Utilities.Search;

/// <summary>
/// Represents a specific field within an object that matched a search query.
/// </summary>
public class SearchField
{
    /// <summary>
    /// Gets or sets the name of the field where the match occurred.
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Gets or sets the original value of the field that was searched.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the relevance score of this specific field within the object.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the list of words that contributed to the match.
    /// </summary>
    public List<SearchWord> Words { get; set; } = new();
}
