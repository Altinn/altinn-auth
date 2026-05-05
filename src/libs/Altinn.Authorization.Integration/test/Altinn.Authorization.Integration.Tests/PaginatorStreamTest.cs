using System.Net;
using System.Text;
using Altinn.Authorization.Integration.Platform;

// See: overhaul part-2 step 10
namespace Altinn.Authorization.Integration.Tests;

/// <summary>
/// Pure-unit tests for <see cref="PaginatorStream{T}"/>. The class is otherwise
/// only exercised via live external Register / ResourceRegister calls, which
/// <c>SkipIfMissingConfiguration</c> in CI without those env configs.
/// </summary>
public class PaginatorStreamTest
{
    private sealed record Item(string Name);

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

    private sealed class TrackingResponse : HttpResponseMessage
    {
        public TrackingResponse(HttpStatusCode statusCode)
            : base(statusCode)
        {
        }

        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private static HttpResponseMessage SuccessPage(string nextUrl)
    {
        var nextJson = nextUrl is null ? "null" : $"\"{nextUrl}\"";
        var body = $$"""{"stats":{"pageStart":0,"pageEnd":0,"sequenceMax":0},"links":{"next":{{nextJson}}},"data":[]}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private static PaginatorStream<Item> NewStream(HttpClient http, HttpResponseMessage seed)
        => new(http, seed, []);

    // ── Termination ────────────────────────────────────────────────────────────
    [Fact]
    public async Task FirstResponseUnsuccessful_YieldsErrorAndStops()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler);
        using var initial = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
        var stream = NewStream(http, initial);

        var pages = new List<PlatformResponse<PageStream<Item>>>();
        await foreach (var page in stream)
        {
            pages.Add(page);
        }

        var single = Assert.Single(pages);
        Assert.False(single.IsSuccessful);
        Assert.Equal(HttpStatusCode.NotFound, single.StatusCode);
        Assert.Empty(handler.CapturedUris);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task LinksNextNullOrEmpty_TerminatesAfterFirstPage(string nextUrl)
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler);
        using var initial = SuccessPage(nextUrl);
        var stream = NewStream(http, initial);

        var pages = new List<PlatformResponse<PageStream<Item>>>();
        await foreach (var page in stream)
        {
            pages.Add(page);
        }

        Assert.Single(pages);
        Assert.True(pages[0].IsSuccessful);
        Assert.Empty(handler.CapturedUris);
    }

    // ── Iteration ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task FollowsLinksNext_FetchesSubsequentPagesUntilNullNext()
    {
        var handler = new CapturingHandler();
        handler.Responses.Enqueue(SuccessPage("https://api.example/page3"));
        handler.Responses.Enqueue(SuccessPage(null));
        using var http = new HttpClient(handler);
        using var initial = SuccessPage("https://api.example/page2");
        var stream = NewStream(http, initial);

        var pages = new List<PlatformResponse<PageStream<Item>>>();
        await foreach (var page in stream)
        {
            pages.Add(page);
        }

        Assert.Equal(3, pages.Count);
        Assert.All(pages, p => Assert.True(p.IsSuccessful));
        Assert.Equal(
            ["https://api.example/page2", "https://api.example/page3"],
            handler.CapturedUris.Select(u => u.ToString()));
    }

    [Fact]
    public async Task UnsuccessfulPageMidStream_YieldsErrorAndStops()
    {
        var handler = new CapturingHandler();
        handler.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") });
        using var http = new HttpClient(handler);
        using var initial = SuccessPage("https://api.example/page2");
        var stream = NewStream(http, initial);

        var pages = new List<PlatformResponse<PageStream<Item>>>();
        await foreach (var page in stream)
        {
            pages.Add(page);
        }

        Assert.Equal(2, pages.Count);
        Assert.True(pages[0].IsSuccessful);
        Assert.False(pages[1].IsSuccessful);
        Assert.Equal(HttpStatusCode.InternalServerError, pages[1].StatusCode);
        Assert.Single(handler.CapturedUris);
    }

    // ── Cancellation ───────────────────────────────────────────────────────────
    [Fact]
    public async Task PreCancelledToken_YieldsNothing()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler);
        using var initial = SuccessPage(null);
        var stream = NewStream(http, initial);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var pages = new List<PlatformResponse<PageStream<Item>>>();
        await foreach (var page in stream.WithCancellation(cts.Token))
        {
            pages.Add(page);
        }

        Assert.Empty(pages);
        Assert.Empty(handler.CapturedUris);
    }

    // ── Resource management ───────────────────────────────────────────────────
    [Fact]
    public async Task PriorResponseDisposed_AfterAdvancingToNextPage()
    {
        var handler = new CapturingHandler();
        handler.Responses.Enqueue(SuccessPage(null));
        using var http = new HttpClient(handler);

        var initial = new TrackingResponse(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"stats":{"pageStart":0,"pageEnd":0,"sequenceMax":0},"links":{"next":"https://api.example/page2"},"data":[]}""",
                Encoding.UTF8,
                "application/json"),
        };

        var stream = NewStream(http, initial);
        Assert.False(initial.Disposed);

        await foreach (var page in stream)
        {
            Assert.NotNull(page);
        }

        Assert.True(initial.Disposed);
    }
}
