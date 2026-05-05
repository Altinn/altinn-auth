using Altinn.AccessMgmt.Core.Authorization;
using Microsoft.AspNetCore.Http;

// See: overhaul part-2 step 13
namespace Altinn.AccessMgmt.Core.Tests.Authorization;

/// <summary>
/// Pure-unit tests for <see cref="ConditionalScope"/> — the predicate factory used
/// by <c>ScopeConditionAuthorizationHandler</c> to decide whether a given access
/// rule applies to the current request based on querystring inspection.
/// </summary>
public class ConditionalScopeTest
{
    private sealed class StubAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }

    private static IHttpContextAccessor AccessorWithQuery(string queryString)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString(queryString);
        return new StubAccessor(ctx);
    }

    [Fact]
    public void QueryParamEquals_NullHttpContext_ReturnsFalse()
    {
        var predicate = ConditionalScope.QueryParamEquals("party", "from");
        var accessor = new StubAccessor(null);

        Assert.False(predicate(accessor));
    }

    [Fact]
    public void QueryParamEquals_FirstParamMissing_ReturnsFalse()
    {
        var predicate = ConditionalScope.QueryParamEquals("party", "from");
        var accessor = AccessorWithQuery("?from=urn:abc");

        Assert.False(predicate(accessor));
    }

    [Fact]
    public void QueryParamEquals_SecondParamMissing_ReturnsFalse()
    {
        var predicate = ConditionalScope.QueryParamEquals("party", "from");
        var accessor = AccessorWithQuery("?party=urn:abc");

        Assert.False(predicate(accessor));
    }

    [Fact]
    public void QueryParamEquals_BothPresentAndEqual_ReturnsTrue()
    {
        var predicate = ConditionalScope.QueryParamEquals("party", "from");
        var accessor = AccessorWithQuery("?party=urn:abc&from=urn:abc");

        Assert.True(predicate(accessor));
    }

    [Fact]
    public void QueryParamEquals_BothPresentButDifferent_ReturnsFalse()
    {
        var predicate = ConditionalScope.QueryParamEquals("party", "from");
        var accessor = AccessorWithQuery("?party=urn:abc&from=urn:def");

        Assert.False(predicate(accessor));
    }

    [Fact]
    public void QueryParamEquals_CaseInsensitiveMatch_ReturnsTrue()
    {
        var predicate = ConditionalScope.QueryParamEquals("party", "from");
        var accessor = AccessorWithQuery("?party=urn:ABC&from=urn:abc");

        Assert.True(predicate(accessor));
    }

    [Fact]
    public void ToOthers_DelegatesToPartyEqualsFrom()
    {
        var predicate = ConditionalScope.ToOthers;

        Assert.True(predicate(AccessorWithQuery("?party=urn:1&from=urn:1")));
        Assert.False(predicate(AccessorWithQuery("?party=urn:1&from=urn:2")));
        Assert.False(predicate(AccessorWithQuery("?party=urn:1&to=urn:1")));
    }

    [Fact]
    public void FromOthers_DelegatesToPartyEqualsTo()
    {
        var predicate = ConditionalScope.FromOthers;

        Assert.True(predicate(AccessorWithQuery("?party=urn:1&to=urn:1")));
        Assert.False(predicate(AccessorWithQuery("?party=urn:1&to=urn:2")));
        Assert.False(predicate(AccessorWithQuery("?party=urn:1&from=urn:1")));
    }
}
