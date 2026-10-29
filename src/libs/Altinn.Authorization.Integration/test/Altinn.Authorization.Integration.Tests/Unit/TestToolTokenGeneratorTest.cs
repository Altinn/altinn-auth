using System.Net;
using System.Text;
using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Tests.Unit;

/// <summary>
/// Pure-unit tests for <see cref="TokenGenerator.TestTool"/>: the request it sends to the test tool's platform access
/// token endpoint, and the options validation for that source.
/// </summary>
[UnitTest]
public class TestToolTokenGeneratorTest
{
    [Theory]
    [InlineData("https://testtool.example", "https://testtool.example/api/GetPlatformAccessToken")]
    [InlineData("https://testtool.example/", "https://testtool.example/api/GetPlatformAccessToken")]
    [InlineData("https://host.example/prefix/", "https://host.example/api/GetPlatformAccessToken")]
    public async Task CreatePlatformAccessToken_WithoutPlatformAccessTokenUrl_CallsTheEndpointsTokenPath(string endpoint, string expected)
    {
        var handler = new CapturingHandler("the-token");
        var generator = new TokenGenerator.TestTool(Options(new Uri(endpoint), null), new SingleClientFactory(handler));

        var token = await generator.CreatePlatformAccessToken("platform", "access-management", TestContext.Current.CancellationToken);

        Assert.Equal("the-token", token);
        AssertRequest(Assert.Single(handler.Requests), expected);
    }

    [Theory]
    [InlineData("http://localtest:8080/mockporten/api/GetPlatformAccessToken")]
    [InlineData("https://host.example/a/b/GetPlatformAccessToken")]
    public async Task CreatePlatformAccessToken_WithPlatformAccessTokenUrl_CallsItAsIs(string url)
    {
        var handler = new CapturingHandler("the-token");
        var generator = new TokenGenerator.TestTool(Options(new Uri("https://ignored.example/"), new Uri(url)), new SingleClientFactory(handler));

        var token = await generator.CreatePlatformAccessToken("platform", "access-management", TestContext.Current.CancellationToken);

        Assert.Equal("the-token", token);
        AssertRequest(Assert.Single(handler.Requests), url);
    }

    [Fact]
    public async Task CreatePlatformAccessToken_PlatformAccessTokenUrlWithAQuery_KeepsItAndAppendsTheParameters()
    {
        var handler = new CapturingHandler("the-token");
        var generator = new TokenGenerator.TestTool(Options(null, new Uri("https://host.example/token?org=ttd")), new SingleClientFactory(handler));

        await generator.CreatePlatformAccessToken("platform", "x", TestContext.Current.CancellationToken);

        Assert.Equal("?org=ttd&env=at22&ttl=3600&app=x", Assert.Single(handler.Requests).RequestUri!.Query);
    }

    [Fact]
    public async Task CreatePlatformAccessToken_FailedResponse_ThrowsUnauthorizedAccessException()
    {
        var handler = new CapturingHandler(null);
        var generator = new TokenGenerator.TestTool(Options(null, new Uri("https://host.example/token")), new SingleClientFactory(handler));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => generator.CreatePlatformAccessToken("platform", "app", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("https://testtool.example/", null)]
    [InlineData(null, "https://host.example/prefix/api/GetPlatformAccessToken")]
    [InlineData("https://testtool.example/", "https://host.example/prefix/api/GetPlatformAccessToken")]
    public void Validation_TestTool_AcceptsEndpointOrPlatformAccessTokenUrl(string? endpoint, string? url)
    {
        var options = Validated(endpoint, url);

        Assert.Equal(AltinnIntegrationOptions.TokenSource.TestTool, options.Value.PlatformAccessToken.TokenSource);
    }

    [Fact]
    public void Validation_TestTool_WithNeitherEndpointNorPlatformAccessTokenUrl_Fails()
    {
        var options = Validated(null, null);

        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Contains(nameof(AltinnIntegrationOptions.PlatformAccessTokenOptions.TestToolOptions.PlatformAccessTokenUrl), exception.Message);
    }

    private static void AssertRequest(HttpRequestMessage request, string expectedPath)
    {
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(expectedPath, request.RequestUri!.GetLeftPart(UriPartial.Path));
        Assert.Equal("?env=at22&ttl=3600&app=access-management", request.RequestUri.Query);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal("user:secret", Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));
    }

    private static IOptions<AltinnIntegrationOptions> Validated(string? endpoint, string? url)
    {
        var services = new ServiceCollection();
        services.AddAltinnPlatformIntegration(opts =>
        {
            opts.PlatformAccessToken.Issuer = "platform";
            opts.PlatformAccessToken.App = "access-management";
            opts.PlatformAccessToken.TokenSource = AltinnIntegrationOptions.TokenSource.TestTool;
            Configure(opts, endpoint is null ? null : new Uri(endpoint), url is null ? null : new Uri(url));
        });
        return services.BuildServiceProvider().GetRequiredService<IOptions<AltinnIntegrationOptions>>();
    }

    private static IOptions<AltinnIntegrationOptions> Options(Uri? endpoint, Uri? url)
    {
        var options = new AltinnIntegrationOptions();
        Configure(options, endpoint, url);
        return Microsoft.Extensions.Options.Options.Create(options);
    }

    private static void Configure(AltinnIntegrationOptions options, Uri? endpoint, Uri? url)
    {
        options.PlatformAccessToken.TestTool.Endpoint = endpoint;
        options.PlatformAccessToken.TestTool.PlatformAccessTokenUrl = url;
        options.PlatformAccessToken.TestTool.Username = "user";
        options.PlatformAccessToken.TestTool.Password = "secret";
        options.PlatformAccessToken.TestTool.Environment = "at22";
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>Records each request and answers 200 with the token, or 401 when it is null.</summary>
    private sealed class CapturingHandler(string? token) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(token is null
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(token) });
        }
    }
}
