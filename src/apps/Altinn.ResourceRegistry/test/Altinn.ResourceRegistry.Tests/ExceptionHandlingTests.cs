using System.Collections.Concurrent;
using System.Net;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Tests.Mocks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.ResourceRegistry.Tests;

/// <summary>
/// Verifies how unhandled exceptions are handled outside of development.
/// </summary>
public class ExceptionHandlingTests(WebApplicationFixture webApplicationFixture)
    : IClassFixture<WebApplicationFixture>
{
    [Fact]
    public async Task UnhandledException_InProduction_ReturnsProblemDetailsAndLogsException()
    {
        var exception = new InvalidOperationException("test exception");
        var logs = new CapturingLoggerProvider();

        var resourceRegistry = new Mock<IResourceRegistry>();
        resourceRegistry
            .Setup(r => r.GetResource(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        await using var app = webApplicationFixture
            .CreateServer(configureServices: services =>
            {
                services.AddSingleton(resourceRegistry.Object);
                services.AddSingleton<ILoggerProvider>(logs);
            })
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        using var client = app.CreateClient();
        using var response = await client.GetAsync("resourceregistry/api/v1/resource/some_resource");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        // ServiceDefaults 6 handles exceptions with an IExceptionHandler, and .NET 10 suppresses the
        // exception handler middleware's diagnostics for handled exceptions by default. Unexpected
        // failures must still be logged with the exception, or they are invisible in production.
        Assert.Contains(logs.Entries, e => e.Level >= LogLevel.Error && ReferenceEquals(e.Exception, exception));
    }
}
