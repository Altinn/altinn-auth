using System.Net.Http.Json;
using System.Text;
using Altinn.Authorization.Integration.Platform;

namespace Altinn.Authorization.Integration.Tests;

/// <summary>
/// Pure-unit tests for <see cref="RequestComposer"/> (no external dependencies).
/// </summary>
public class RequestComposerTest
{
    // ── New ──────────────────────────────────────────────────────────────────
    [Fact]
    public void New_NoActions_ReturnsEmptyHttpRequestMessage()
    {
        var request = RequestComposer.New();
        Assert.NotNull(request);
        Assert.Null(request.RequestUri);
    }

    [Fact]
    public void New_WithMultipleActions_AppliesEachAction()
    {
        var request = RequestComposer.New(
            RequestComposer.WithHttpVerb(HttpMethod.Post),
            RequestComposer.WithSetUri("https://example.com/api"));

        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://example.com/api", request.RequestUri!.ToString());
    }

    // ── WithHttpVerb ─────────────────────────────────────────────────────────
    [Fact]
    public void WithHttpVerb_SetsMethod()
    {
        var request = RequestComposer.New(RequestComposer.WithHttpVerb(HttpMethod.Delete));
        Assert.Equal(HttpMethod.Delete, request.Method);
    }

    // ── WithSetUri (string) ──────────────────────────────────────────────────
    [Fact]
    public void WithSetUri_String_SetsUri()
    {
        var request = RequestComposer.New(RequestComposer.WithSetUri("https://test.example/path"));
        Assert.Equal("https://test.example/path", request.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithSetUri_NullOrEmpty_DoesNotSetUri(string? uri)
    {
        var request = RequestComposer.New(RequestComposer.WithSetUri(uri!));
        Assert.Null(request.RequestUri);
    }

    // ── WithSetUri (Uri + segments) ───────────────────────────────────────────
    [Fact]
    public void WithSetUri_BaseUriWithSegments_CombinesPath()
    {
        var baseUri = new Uri("https://example.com/");
        var request = RequestComposer.New(RequestComposer.WithSetUri(baseUri, "v1", "resource"));
        Assert.Contains("v1/resource", request.RequestUri!.ToString());
    }

    [Fact]
    public void WithSetUri_NullBaseUri_DoesNotSetUri()
    {
        var request = RequestComposer.New(RequestComposer.WithSetUri((Uri)null!, "segment"));
        Assert.Null(request.RequestUri);
    }

    // ── WithJSONPayload ───────────────────────────────────────────────────────
    [Fact]
    public void WithJSONPayload_NonNull_SetsJsonContent()
    {
        var payload = new { Name = "test", Value = 42 };
        var request = RequestComposer.New(RequestComposer.WithJSONPayload(payload));
        Assert.NotNull(request.Content);
    }

    [Fact]
    public void WithJSONPayload_Null_DoesNotSetContent()
    {
        var request = RequestComposer.New(RequestComposer.WithJSONPayload<string>(null!));
        Assert.Null(request.Content);
    }

    // ── WithAppendQueryParam (IEnumerable) ────────────────────────────────────
    [Fact]
    public void WithAppendQueryParam_Enumerable_AppendsCommaSeparatedValues()
    {
        var request = RequestComposer.New(
            RequestComposer.WithSetUri("https://example.com/api"),
            RequestComposer.WithAppendQueryParam("ids", (IEnumerable<string>)["a", "b", "c"]));

        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
        Assert.Equal("a,b,c", query["ids"]);
    }

    [Fact]
    public void WithAppendQueryParam_EmptyEnumerable_DoesNotAppendQuery()
    {
        var request = RequestComposer.New(
            RequestComposer.WithSetUri("https://example.com/api"),
            RequestComposer.WithAppendQueryParam("ids", (IEnumerable<string>)[]));

        Assert.Empty(request.RequestUri!.Query);
    }

    [Fact]
    public void WithAppendQueryParam_NullEnumerable_DoesNotAppendQuery()
    {
        var request = RequestComposer.New(
            RequestComposer.WithSetUri("https://example.com/api"),
            RequestComposer.WithAppendQueryParam("ids", (IEnumerable<string>)null!));

        Assert.Empty(request.RequestUri!.Query);
    }

    // ── WithAppendQueryParam (single value) ───────────────────────────────────
    [Fact]
    public void WithAppendQueryParam_SingleValue_AppendsQuery()
    {
        var request = RequestComposer.New(
            RequestComposer.WithSetUri("https://example.com/api"),
            RequestComposer.WithAppendQueryParam("page", 2));

        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
        Assert.Equal("2", query["page"]);
    }

    [Fact]
    public void WithAppendQueryParam_DefaultIntValue_DoesNotAppendQuery()
    {
        var request = RequestComposer.New(
            RequestComposer.WithSetUri("https://example.com/api"),
            RequestComposer.WithAppendQueryParam("page", default(int)));

        Assert.Empty(request.RequestUri!.Query);
    }

    // ── WithPlatformAccessToken (string) ──────────────────────────────────────
    [Fact]
    public void WithPlatformAccessToken_NonEmpty_SetsHeader()
    {
        var request = RequestComposer.New(RequestComposer.WithPlatformAccessToken("my-token"));
        Assert.True(request.Headers.Contains("PlatformAccessToken"));
        Assert.Equal("my-token", request.Headers.GetValues("PlatformAccessToken").Single());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithPlatformAccessToken_NullOrEmpty_DoesNotSetHeader(string? token)
    {
        var request = RequestComposer.New(RequestComposer.WithPlatformAccessToken(token!));
        Assert.False(request.Headers.Contains("PlatformAccessToken"));
    }

    // ── WithPlatformAccessToken (Func) ────────────────────────────────────────
    [Fact]
    public void WithPlatformAccessToken_Func_SetsHeader()
    {
        var request = RequestComposer.New(
            RequestComposer.WithPlatformAccessToken(() => Task.FromResult("func-token")));
        Assert.Equal("func-token", request.Headers.GetValues("PlatformAccessToken").Single());
    }

    // ── WithJWTToken ──────────────────────────────────────────────────────────
    [Fact]
    public void WithJWTToken_NonEmpty_SetsAuthorizationBearerHeader()
    {
        var request = RequestComposer.New(RequestComposer.WithJWTToken("jwt-value"));
        Assert.True(request.Headers.Contains("Authorization"));
        Assert.Equal("Bearer jwt-value", request.Headers.GetValues("Authorization").Single());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithJWTToken_NullOrEmpty_DoesNotSetHeader(string? token)
    {
        var request = RequestComposer.New(RequestComposer.WithJWTToken(token!));
        Assert.False(request.Headers.Contains("Authorization"));
    }
}
