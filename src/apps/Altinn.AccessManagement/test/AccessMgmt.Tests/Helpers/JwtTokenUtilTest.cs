using Altinn.AccessManagement.Core.Helpers;
using Microsoft.AspNetCore.Http;

// See: overhaul part-2 step 17
namespace Altinn.AccessManagement.Tests.Helpers;

/// <summary>
/// Pure-unit tests for <see cref="JwtTokenUtil.GetTokenFromContext"/>.
/// Pins the cookie-first / Authorization-header-fallback resolution chain
/// and the "Bearer " prefix stripping rules.
/// </summary>
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
    public void CookiePresent_ReturnsCookieValue()
    {
        var ctx = CtxWithCookie(CookieName, "cookie-token-value");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("cookie-token-value");
    }

    [Fact]
    public void NoCookieNoAuthHeader_ReturnsEmpty()
    {
        JwtTokenUtil.GetTokenFromContext(new DefaultHttpContext(), CookieName).Should().BeEmpty();
    }

    [Fact]
    public void NoCookieBearerHeader_ReturnsTokenWithoutPrefix()
    {
        var ctx = CtxWithAuthHeader("Bearer my-jwt-token");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("my-jwt-token");
    }

    [Fact]
    public void NoCookieBearerHeaderLowercase_AlsoMatchesViaCaseInsensitiveStartsWith()
    {
        // Production code uses StringComparison.OrdinalIgnoreCase on StartsWith,
        // so "bearer " and "BEARER " both match — pin this behavior.
        var ctx = CtxWithAuthHeader("bearer my-jwt-token");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("my-jwt-token");
    }

    [Fact]
    public void NoCookieBearerHeader_ExtraWhitespaceTrimmed()
    {
        var ctx = CtxWithAuthHeader("Bearer    my-jwt-token   ");
        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("my-jwt-token");
    }

    [Fact]
    public void NoCookieNonBearerAuthHeader_ReturnsNullOrEmpty()
    {
        // Edge case: a non-Bearer Authorization header is silently dropped.
        // Pinning current behavior — token remains null after the cookie miss
        // and the Bearer branch never assigns it.
        var ctx = CtxWithAuthHeader("Basic dXNlcjpwYXNz");
        var result = JwtTokenUtil.GetTokenFromContext(ctx, CookieName);
        Assert.True(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void EmptyCookieValue_FallsBackToAuthHeader()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Cookie = $"{CookieName}=";
        ctx.Request.Headers.Authorization = "Bearer fallback-token";

        JwtTokenUtil.GetTokenFromContext(ctx, CookieName).Should().Be("fallback-token");
    }
}
