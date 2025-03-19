using Altinn.AccessMgmt.Persistence;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.FFB.Models;

/// <summary>
/// User data
/// </summary>
public static class UserData
{
    /// <summary>
    /// User Language
    /// </summary>
    public static string Language { get; set; }

    /// <summary>
    /// AsOf
    /// </summary>
    public static DateTimeOffset? AsOf { get; set; }

    /// <summary>
    /// Generate RequestOptions
    /// </summary>
    /// <returns></returns>
    public static RequestOptions GetRequestOptions()
    {
        var res = new RequestOptions();
        if (!string.IsNullOrEmpty(Language))
        {
            res.Language = Language;
        }

        if (AsOf.HasValue)
        {
            res.AsOf = AsOf.Value;
        }

        return res;
    }
}
