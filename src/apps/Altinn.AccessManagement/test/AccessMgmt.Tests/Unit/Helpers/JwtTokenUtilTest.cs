using Altinn.AccessManagement.Core.Helpers;
using Microsoft.AspNetCore.Http;

// See: overhaul part-2 step 17
namespace Altinn.AccessManagement.Tests.Unit.Helpers;

/// <summary>
/// Pure-unit tests for <see cref="JwtTokenUtil.GetTokenFromContext"/>.
/// Pins the cookie-first / Authorization-header-fallback resolution chain
/// and the "Bearer " prefix stripping rules.
/// </summary>
[UnitTest]
public class JwtTokenUtilTest
{
    private const string CookieName = "AltinnStudioRuntime";

    private static HttpContext CtxWithCookie(string cookieName, string value)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Cookie = $"{cookieName}={value}";
        return ctx;
    }

    private static HttpContext CtxWithAuthHeader(string headerValue)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = headerValue;
        return ctx;
    }

    [Fact]
    public void GetTokenFromContext_CookiePresent_ReturnsCookieValue()
    {
        var ctx = CtxWithCookie(CookieName, "cookie-token-value");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("cookie-token-value");
    }

    [Fact]
    public void GetTokenFromContext_NoCookieNoAuthHeader_ReturnsEmpty()
    {
        JwtTokenUtil.GetTokenFromContext(new DefaultHttpContext(), CookieName).Should().BeEmpty();
    }

    [Fact]
    public void GetTokenFromContext_NoCookieBearerHeader_ReturnsTokenWithoutPrefix()
    {
        var ctx = CtxWithAuthHeader("Bearer my-jwt-token");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("my-jwt-token");
    }

    [Fact]
    public void GetTokenFromContext_NoCookieLowercaseBearerHeader_ReturnsTokenWithoutPrefix()
    {
        // Production code uses StringComparison.OrdinalIgnoreCase on StartsWith,
        // so "bearer " and "BEARER " both match — pin this behavior.
        var ctx = CtxWithAuthHeader("bearer my-jwt-token");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("my-jwt-token");
    }

    [Fact]
    public void GetTokenFromContext_NoCookieBearerHeaderWithExtraWhitespace_ReturnsTrimmedToken()
    {
        var ctx = CtxWithAuthHeader("Bearer    my-jwt-token   ");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("my-jwt-token");
    }

    [Fact]
    public void GetTokenFromContext_NoCookieNonBearerAuthHeader_ReturnsNullOrEmpty()
    {
        // Edge case: a non-Bearer Authorization header is silently dropped.
        // Pinning current behavior — token remains null after the cookie miss
        // and the Bearer branch never assigns it.
        var ctx = CtxWithAuthHeader("Basic dXNlcjpwYXNz");
        var result = JwtTokenUtil.GetTokenFromContext(ctx, CookieName);
        Assert.True(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetTokenFromContext_EmptyCookieValue_FallsBackToAuthHeader()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Cookie = $"{CookieName}=";
        ctx.Request.Headers.Authorization = "Bearer fallback-token";

        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("fallback-token");
    }
}
