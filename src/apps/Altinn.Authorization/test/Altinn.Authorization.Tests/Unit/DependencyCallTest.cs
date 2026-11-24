using System.Net;
using Altinn.Platform.Authorization.Exceptions;
using Altinn.Platform.Authorization.Helpers;

namespace Altinn.Authorization.Tests.Unit;

/// <summary>
/// Tests for <see cref="DependencyCall"/>: which answers from a dependency are failures, and which are answers.
/// </summary>
[UnitTest]
public class DependencyCallTest
{
    private const string Dependency = "some-service";

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task SendAsync_ServiceAnswered_ReturnsResponse(HttpStatusCode statusCode)
    {
        using HttpResponseMessage response = await DependencyCall.SendAsync(Dependency, () => Task.FromResult(new HttpResponseMessage(statusCode)), TestContext.Current.CancellationToken);

        Assert.Equal(statusCode, response.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Redirect)]
    public async Task SendAsync_ServiceFailed_ThrowsWithDependencyAndStatusCode(HttpStatusCode statusCode)
    {
        DependencyFailedException ex = await Assert.ThrowsAsync<DependencyFailedException>(
            () => DependencyCall.SendAsync(Dependency, () => Task.FromResult(new HttpResponseMessage(statusCode)), TestContext.Current.CancellationToken));

        Assert.Equal(Dependency, ex.Dependency);
        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Contains(((int)statusCode).ToString(), ex.Message);
    }

    [Fact]
    public async Task SendAsync_ConnectionFails_ThrowsWithoutStatusCode()
    {
        HttpRequestException cause = new("Connection refused");

        DependencyFailedException ex = await Assert.ThrowsAsync<DependencyFailedException>(
            () => DependencyCall.SendAsync(Dependency, () => Task.FromException<HttpResponseMessage>(cause), TestContext.Current.CancellationToken));

        Assert.Equal(Dependency, ex.Dependency);
        Assert.Null(ex.StatusCode);
        Assert.Same(cause, ex.InnerException);
    }

    [Fact]
    public async Task SendAsync_ClientTimesOut_ThrowsDependencyFailed()
    {
        // HttpClient reports its own timeout as a cancellation, although nobody cancelled the caller's token.
        TaskCanceledException timeout = new("The request was canceled due to the configured HttpClient.Timeout", new TimeoutException());

        DependencyFailedException ex = await Assert.ThrowsAsync<DependencyFailedException>(
            () => DependencyCall.SendAsync(Dependency, () => Task.FromException<HttpResponseMessage>(timeout), TestContext.Current.CancellationToken));

        Assert.Equal(Dependency, ex.Dependency);
        Assert.Null(ex.StatusCode);
        Assert.Same(timeout, ex.InnerException);
    }

    [Fact]
    public async Task SendAsync_CallerCancels_CancellationIsNotAFailedLookup()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DependencyCall.SendAsync(Dependency, () => Task.FromCanceled<HttpResponseMessage>(cts.Token), cts.Token));
    }

    [Fact]
    public async Task SendAsync_ServiceFailed_MessageLeavesOutTheResponseBody()
    {
        HttpResponseMessage response = new(HttpStatusCode.InternalServerError) { Content = new StringContent("person 01017012345 not readable") };

        DependencyFailedException ex = await Assert.ThrowsAsync<DependencyFailedException>(
            () => DependencyCall.SendAsync(Dependency, () => Task.FromResult(response), TestContext.Current.CancellationToken));

        Assert.DoesNotContain("01017012345", ex.Message);
    }
}
