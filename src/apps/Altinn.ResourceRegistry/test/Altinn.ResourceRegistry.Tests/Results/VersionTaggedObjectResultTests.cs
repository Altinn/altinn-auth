using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.Results;
using Microsoft.AspNetCore.Mvc.Formatters;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Tests.Results;

public class VersionTaggedObjectResultTests
{
    [Fact]
    public async Task SetsStatusCode()
    {
        var result = new VersionedTaggedObjectResult("value", null, null)
        {
            Formatters = [new NoOpOutputFormatter()],
        };

        var context = GetActionContext();
        await result.ExecuteResultAsync(context);

        context.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task SetsETagHeader()
    {
        var result = new VersionedTaggedObjectResult("value", "tag", null)
        {
            Formatters = [new NoOpOutputFormatter()],
        };

        var context = GetActionContext();
        await result.ExecuteResultAsync(context);

        context.HttpContext.Response.Headers.ETag.Should().ContainSingle().Which.Should().Be("tag");
    }

    [Fact]
    public async Task SetsLastModifiedHeader()
    {
        var result = new VersionedTaggedObjectResult("value", null, new HttpDateTimeHeaderValue(new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero)))
        {
            Formatters = [new NoOpOutputFormatter()],
        };

        var context = GetActionContext();
        await result.ExecuteResultAsync(context);

        context.HttpContext.Response.Headers.LastModified.Should().ContainSingle().Which.Should().Be("Fri, 01 Jan 2021 01:01:01 GMT");
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        var options = Options.Create(new MvcOptions());
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
            new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
            new TestHttpResponseStreamWriterFactory(),
            NullLoggerFactory.Instance,
            options));
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }

    private static ActionContext GetActionContext()
    {
        var httpContext = GetHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor();
        return new ActionContext(httpContext, routeData, actionDescriptor);
    }

    private class NoOpOutputFormatter : IOutputFormatter
    {
        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return true;
        }

        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            return Task.FromResult(0);
        }
    }
}
