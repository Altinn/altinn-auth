using System.Net;
using Altinn.Platform.Authorization.Extensions;

namespace Altinn.Platform.Authorization.Tests.ExtensionTests;

public class HttpClientExtensionTest
{
    // --- PostAsync ---
    [Fact]
    public async Task PostAsync_WithNoTokens_SendsRequestWithoutAuthHeaders()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.PostAsync("/api/test", new StringContent("body"), TestContext.Current.CancellationToken);

        Assert.NotNull(handler.Request);
        Assert.False(handler.Request.Headers.Contains("Authorization"));
        Assert.False(handler.Request.Headers.Contains("PlatformAccessToken"));
    }

    [Fact]
    public async Task PostAsync_WithAuthToken_SetsAuthorizationHeader()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.PostAsync("/api/test", new StringContent("body"), authorizationToken: "jwt-token", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handler.Request.Headers.Contains("Authorization"));
        Assert.Equal("Bearer jwt-token", handler.Request.Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task PostAsync_WithPlatformAccessToken_SetsPlatformHeader()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.PostAsync("/api/test", new StringContent("body"), platformAccessToken: "platform-token", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handler.Request.Headers.Contains("PlatformAccessToken"));
        Assert.Equal("platform-token", handler.Request.Headers.GetValues("PlatformAccessToken").Single());
    }

    [Fact]
    public async Task PostAsync_WithBothTokens_SetsBothHeaders()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.PostAsync("/api/test", new StringContent("body"), "jwt-token", "platform-token", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handler.Request.Headers.Contains("Authorization"));
        Assert.True(handler.Request.Headers.Contains("PlatformAccessToken"));
    }

    [Fact]
    public async Task PostAsync_UsesPostMethod()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.PostAsync("/api/test", new StringContent("body"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.Request.Method);
    }

    // --- GetAsync ---
    [Fact]
    public async Task GetAsync_WithNoTokens_SendsRequestWithoutAuthHeaders()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.GetAsync("https://example.com/api/test", TestContext.Current.CancellationToken);

        Assert.NotNull(handler.Request);
        Assert.False(handler.Request.Headers.Contains("Authorization"));
        Assert.False(handler.Request.Headers.Contains("PlatformAccessToken"));
    }

    [Fact]
    public async Task GetAsync_WithAuthToken_SetsAuthorizationHeader()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.GetAsync("https://example.com/api/test", authorizationToken: "jwt-token", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handler.Request.Headers.Contains("Authorization"));
        Assert.Equal("Bearer jwt-token", handler.Request.Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task GetAsync_WithPlatformAccessToken_SetsPlatformHeader()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.GetAsync("https://example.com/api/test", platformAccessToken: "platform-token", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handler.Request.Headers.Contains("PlatformAccessToken"));
        Assert.Equal("platform-token", handler.Request.Headers.GetValues("PlatformAccessToken").Single());
    }

    [Fact]
    public async Task GetAsync_UsesGetMethod()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };

        await client.GetAsync("https://example.com/api/test", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.Request.Method);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public HttpRequestMessage Request { get; private set; }

        public RecordingHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(_response);
        }
    }
}
