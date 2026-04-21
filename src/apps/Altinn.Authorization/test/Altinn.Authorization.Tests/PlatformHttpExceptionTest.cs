using System.Net;
using Altinn.Platform.Authorization.Exceptions;

namespace Altinn.Platform.Authorization.Tests;

public class PlatformHttpExceptionTest
{
    [Fact]
    public async Task CreateAsync_FormatsMessageWithStatusCodeAndContent()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Not Found",
            Content = new StringContent("resource missing"),
        };

        var ex = await PlatformHttpException.CreateAsync(response);

        Assert.Contains("404", ex.Message);
        Assert.Contains("Not Found", ex.Message);
        Assert.Contains("resource missing", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_StoresResponseOnException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "Internal Server Error",
            Content = new StringContent(string.Empty),
        };

        var ex = await PlatformHttpException.CreateAsync(response);

        Assert.Same(response, ex.Response);
    }

    [Fact]
    public void Constructor_SetsMessageAndResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var ex = new PlatformHttpException(response, "test message");

        Assert.Equal("test message", ex.Message);
        Assert.Same(response, ex.Response);
    }

    [Fact]
    public void PlatformHttpException_IsException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var ex = new PlatformHttpException(response, "msg");

        Assert.IsAssignableFrom<Exception>(ex);
    }
}
