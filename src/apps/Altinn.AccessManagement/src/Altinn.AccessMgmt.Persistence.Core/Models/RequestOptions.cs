namespace Altinn.AccessMgmt.Persistence.Core.Models;

/// <summary>
/// RequestOptions
/// </summary>
public class RequestOptions
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// AsOf
    /// </summary>
    public DateTimeOffset? AsOf { get; set; }

    /// <summary>
    /// OrderBy
    /// </summary>
    public string OrderBy { get; set; }

    /// <summary>
    /// UsePaging (default: false)
    /// </summary>
    public bool UsePaging { get; set; } = false;

    /// <summary>
    /// PageSize (default: 25)
    /// </summary>
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// PageNumber (default: 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
}
