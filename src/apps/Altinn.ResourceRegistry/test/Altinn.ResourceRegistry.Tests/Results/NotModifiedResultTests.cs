using Altinn.ResourceRegistry.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Tests.Results;

public class NotModifiedResultTests
{
    [Fact]
    public async Task SetsStatusCode()
    {
        var result = new NotModifiedResult(null, null);

        var context = GetActionContext();
        await result.ExecuteResultAsync(context);

        context.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact]
    public async Task SetsETagHeader()
    {
        var result = new NotModifiedResult("tag", null);

        var context = GetActionContext();
        await result.ExecuteResultAsync(context);

        context.HttpContext.Response.Headers.ETag.Should().ContainSingle().Which.Should().Be("tag");
    }

    [Fact]
    public async Task SetsLastModifiedHeader()
    {
        var result = new NotModifiedResult(null, new HttpDateTimeHeaderValue(new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero)));

        var context = GetActionContext();
        await result.ExecuteResultAsync(context);

        context.HttpContext.Response.Headers.LastModified.Should().ContainSingle().Which.Should().Be("Fri, 01 Jan 2021 01:01:01 GMT");
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
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
}
