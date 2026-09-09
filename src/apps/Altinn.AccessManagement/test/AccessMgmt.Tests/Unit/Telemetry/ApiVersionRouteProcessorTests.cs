using System.Diagnostics;
using Altinn.AccessManagement.Telemetry;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessManagement.Tests.Unit.Telemetry;

/// <summary>
/// Unit tests for <see cref="ApiVersionRouteProcessor"/>, which resolves API version route parameters
/// in the route recorded on request telemetry.
/// </summary>
[UnitTest]
public class ApiVersionRouteProcessorTests : IDisposable
{
    private const string ActivitySourceName = "Altinn.AccessManagement.Tests.ApiVersionRouteProcessor";

    private readonly ActivityListener _listener;
    private readonly ActivitySource _source = new(ActivitySourceName);

    public ApiVersionRouteProcessorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void OnEnd_VersionedRoute_ReplacesVersionParameterWithRequestedVersion()
    {
        const string Template = "accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations/my/clients";
        const string Expected = "accessmanagement/api/v2/enduser/clientdelegations/my/clients";

        using var activity = StartServerActivity(Template);
        Processor(("version", "2")).OnEnd(activity);

        activity.GetTagItem("http.route").Should().Be(Expected);
        activity.DisplayName.Should().Be($"GET {Expected}");
    }

    [Fact]
    public void OnEnd_VersionedRouteWithOtherParameters_KeepsOtherParametersAsPlaceholders()
    {
        const string Template = "accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations/{delegationId}";
        const string Expected = "accessmanagement/api/v2/enduser/clientdelegations/{delegationId}";

        using var activity = StartServerActivity(Template);
        Processor(("version", "2"), ("delegationId", "e5b6d1ad-8c4d-4e4a-9f6c-6dbb9b1a8d10")).OnEnd(activity);

        activity.GetTagItem("http.route").Should().Be(Expected);
    }

    [Fact]
    public void OnEnd_UnversionedRoute_LeavesRouteUnchanged()
    {
        const string Template = "accessmanagement/api/v1/enduser/clientdelegations/clients";

        using var activity = StartServerActivity(Template);
        Processor(("version", "2")).OnEnd(activity);

        activity.GetTagItem("http.route").Should().Be(Template);
        activity.DisplayName.Should().Be($"GET {Template}");
    }

    [Fact]
    public void OnEnd_NoHttpContext_LeavesRouteUnchanged()
    {
        const string Template = "accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations/my/clients";

        using var activity = StartServerActivity(Template);
        new ApiVersionRouteProcessor(new HttpContextAccessor()).OnEnd(activity);

        activity.GetTagItem("http.route").Should().Be(Template);
    }

    [Fact]
    public void OnEnd_MissingVersionRouteValue_LeavesRouteUnchanged()
    {
        const string Template = "accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations/my/clients";

        using var activity = StartServerActivity(Template);
        Processor().OnEnd(activity);

        activity.GetTagItem("http.route").Should().Be(Template);
    }

    [Fact]
    public void OnEnd_ClientActivity_LeavesRouteUnchanged()
    {
        const string Template = "accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations/my/clients";

        using var activity = _source.StartActivity($"GET {Template}", ActivityKind.Client);
        activity.SetTag("http.route", Template);

        Processor(("version", "2")).OnEnd(activity);

        activity.GetTagItem("http.route").Should().Be(Template);
    }

    public void Dispose()
    {
        _source.Dispose();
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    private Activity StartServerActivity(string route)
    {
        var activity = _source.StartActivity($"GET {route}", ActivityKind.Server);
        activity.Should().NotBeNull();
        activity.SetTag("http.route", route);
        return activity;
    }

    private static ApiVersionRouteProcessor Processor(params (string Key, string Value)[] routeValues)
    {
        var httpContext = new DefaultHttpContext();
        foreach (var (key, value) in routeValues)
        {
            httpContext.Request.RouteValues[key] = value;
        }

        return new ApiVersionRouteProcessor(new HttpContextAccessor { HttpContext = httpContext });
    }
}
