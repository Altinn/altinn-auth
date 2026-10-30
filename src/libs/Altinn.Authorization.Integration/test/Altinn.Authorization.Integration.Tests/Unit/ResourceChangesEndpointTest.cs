using System.Net;
using System.Text;
using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="AltinnResourceRegistryClient.StreamResourceChanges"/>, using a fake HTTP handler
/// that answers with the page shape the Resource Registry returns for <c>resource/changes</c>.
/// </summary>
[UnitTest]
public class ResourceChangesEndpointTest
{
    private const string Endpoint = "https://platform.test/";

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses { get; } = new();

        public List<Uri> CapturedUris { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapturedUris.Add(request.RequestUri);
            return Task.FromResult(Responses.Dequeue());
        }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static HttpResponseMessage ChangesPage(string nextUrl, params string[] resourceIds)
    {
        var nextJson = nextUrl is null ? "null" : $"\"{nextUrl}\"";
        var items = string.Join(",", resourceIds.Select(id => $$"""{"resourceId":"{{id}}","changedAt":"2026-10-01T12:00:00+00:00"}"""));
        var body = $$"""{"links":{"next":{{nextJson}}},"data":[{{items}}]}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private static AltinnResourceRegistryClient NewClient(HttpMessageHandler handler) => new(
        new FakeHttpClientFactory(handler),
        Options.Create(new AltinnResourceRegistryOptions { Endpoint = new Uri(Endpoint) }),
        Options.Create(new AltinnIntegrationOptions()));

    [Fact]
    public async Task StreamResourceChanges_WithoutNextPage_StartsFromBeginningAndFollowsLinks()
    {
        var next1 = $"{Endpoint}resourceregistry/api/v1/resource/changes?token=abc&limit=100";
        var next2 = $"{Endpoint}resourceregistry/api/v1/resource/changes?token=def&limit=100";
        var firstPageIds = Enumerable.Range(1, 100).Select(i => $"resource-{i}").ToArray();
        var secondPageIds = Enumerable.Range(101, 50).Select(i => $"resource-{i}").ToArray();
        var handler = new CapturingHandler();
        handler.Responses.Enqueue(ChangesPage(next1, firstPageIds));
        handler.Responses.Enqueue(ChangesPage(next2, secondPageIds));
        handler.Responses.Enqueue(ChangesPage(null, []));

        var pages = new List<PlatformResponse<PageStream<ResourceChangeModel>>>();
        await foreach (var page in await NewClient(handler).StreamResourceChanges(cancellationToken: TestContext.Current.CancellationToken))
        {
            pages.Add(page);
        }

        Assert.Equal(3, pages.Count);
        Assert.All(pages, p => Assert.True(p.IsSuccessful));
        Assert.Equal(firstPageIds, pages[0].Content.Data.Select(c => c.ResourceId));
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), pages[0].Content.Data.First().ChangedAt);
        Assert.Equal(next1, pages[0].Content.Links.Next);
        Assert.Equal(secondPageIds, pages[1].Content.Data.Select(c => c.ResourceId));
        Assert.Equal(next2, pages[1].Content.Links.Next);
        Assert.Equal([], pages[2].Content.Data.Select(c => c.ResourceId));
        Assert.Null(pages[2].Content.Links.Next);

        Assert.Equal(
            [
                new Uri($"{Endpoint}resourceregistry/api/v1/resource/changes?limit=100"),
                new Uri(next1),
                new Uri(next2),
            ],
            handler.CapturedUris);
    }

    [Fact]
    public async Task StreamResourceChanges_WithNextPage_ResumesFromStoredLink()
    {
        var stored = $"{Endpoint}resourceregistry/api/v1/resource/changes?token=abc&limit=100";
        var handler = new CapturingHandler();
        handler.Responses.Enqueue(ChangesPage(null));

        var pages = new List<PlatformResponse<PageStream<ResourceChangeModel>>>();
        await foreach (var page in await NewClient(handler).StreamResourceChanges(stored, TestContext.Current.CancellationToken))
        {
            pages.Add(page);
        }

        var single = Assert.Single(pages);
        Assert.True(single.IsSuccessful);
        Assert.Empty(single.Content.Data);
        Assert.Equal([new Uri(stored)], handler.CapturedUris);
    }

    [Fact]
    public async Task StreamResourceChanges_WhenRegistryFails_YieldsProblemAndStops()
    {
        var handler = new CapturingHandler();
        handler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{}") });

        var pages = new List<PlatformResponse<PageStream<ResourceChangeModel>>>();
        await foreach (var page in await NewClient(handler).StreamResourceChanges(cancellationToken: TestContext.Current.CancellationToken))
        {
            pages.Add(page);
        }

        var single = Assert.Single(pages);
        Assert.False(single.IsSuccessful);
        Assert.Equal(HttpStatusCode.BadRequest, single.StatusCode);
    }
}
