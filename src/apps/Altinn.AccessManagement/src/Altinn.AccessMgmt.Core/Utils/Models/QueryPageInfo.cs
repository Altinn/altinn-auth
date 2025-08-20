namespace Altinn.AccessMgmt.Core.Utils.Models;

/// <summary>
/// Query pageing information
/// </summary>
public class QueryPageInfo
{
    /// <summary>
    /// Current page
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Intended page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total result size
    /// </summary>
    public int TotalSize { get; set; }

    /// <summary>
    /// First rownumber on page
    /// </summary>
    public int FirstRowOnPage { get; set; }

    /// <summary>
    /// Last rownumber on page
    /// </summary>
    public int LastRowOnPage { get; set; }
}
