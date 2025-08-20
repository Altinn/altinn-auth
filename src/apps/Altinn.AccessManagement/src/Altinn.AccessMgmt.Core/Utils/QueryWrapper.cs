using Altinn.AccessMgmt.Core.Utils.Models;

namespace Altinn.AccessMgmt.Core.Utils;

internal static class QueryWrapper
{
    public static QueryResponse<T> WrapQueryResponse<T>(IEnumerable<T> data)
    {
        var resultCount = data.Count();
        return new QueryResponse<T> 
        { 
            Data = data,
            Page = new QueryPageInfo()
            {
                FirstRowOnPage = 0,
                LastRowOnPage = resultCount,
                PageNumber = 1,
                PageSize = resultCount,
                TotalSize = resultCount
            }
        };
    }
}
