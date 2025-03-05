namespace Altinn.AccessMgmt.Persistence.Core.Utilities.Search;

/// <summary>
/// Represents an individual word in a search field and its match status.
/// </summary>
public class SearchWord
{
    /// <summary>
    /// Gets or sets the original word content from the field.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the lowercase version of the word for case-insensitive matching.
    /// </summary>
    public string LowercaseContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this word matched the search term.
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// Gets or sets the match score for this word.
    /// </summary>
    public double Score { get; set; }
}
