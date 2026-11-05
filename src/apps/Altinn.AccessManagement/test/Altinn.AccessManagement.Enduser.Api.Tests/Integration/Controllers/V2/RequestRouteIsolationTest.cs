using Altinn.AccessManagement.TestUtils.Fixtures;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using ControllersV1 = Altinn.AccessManagement.Api.Enduser.Controllers;
using ControllersV2 = Altinn.AccessManagement.Api.Enduser.Controllers.V2;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers.V2;

/// <summary>
/// Guards that the v1 and v2 request controllers do not leak into each other. Both inherit the shared
/// actions from the request base controller and set their own class route, so every action must resolve
/// under its own route only, and each version must have exactly one action for creating a resource request.
/// </summary>
[IntegrationTest]
public class RequestRouteIsolationTest : IClassFixture<ApiFixture>
{
    private const string RequestV1Route = "accessmanagement/api/v1/enduser/request";
    private const string RequestV2Route = "accessmanagement/api/v2/enduser/request";

    public RequestRouteIsolationTest(ApiFixture fixture)
    {
        fixture.BuildConfiguration();
        Services = fixture.Services;
    }

    private IServiceProvider Services { get; }

    public static TheoryData<Type, string> ControllerRoutes => new()
    {
        { typeof(ControllersV1.RequestController), RequestV1Route },
        { typeof(ControllersV2.RequestController), RequestV2Route },
    };

    [Theory]
    [MemberData(nameof(ControllerRoutes))]
    public void Controller_OnlyExposesActionsUnderItsOwnRoute(Type controllerType, string expectedRoute)
    {
        var paths = GetActions(controllerType).Select(a => a.RelativePath).ToList();

        paths.Should().NotBeEmpty();
        paths.Should().OnlyContain(p => p == expectedRoute || p.StartsWith($"{expectedRoute}/", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ControllerRoutes))]
    public void Controller_HasOneCreateResourceRequestAction(Type controllerType, string expectedRoute)
    {
        var actions = GetActions(controllerType)
            .Where(a => a.HttpMethod == "POST" && a.RelativePath == $"{expectedRoute}/resource")
            .ToList();

        actions.Should().ContainSingle();
    }

    [Fact]
    public void V1AndV2_ExposeTheSameActions()
    {
        static List<string> Signatures(IEnumerable<(string HttpMethod, string ActionName, string RelativePath)> actions, string route) =>
            [.. actions.Select(a => $"{a.HttpMethod} {a.RelativePath[route.Length..]}").Order()];

        var v1 = Signatures(GetActions(typeof(ControllersV1.RequestController)), RequestV1Route);
        var v2 = Signatures(GetActions(typeof(ControllersV2.RequestController)), RequestV2Route);

        v2.Should().Equal(v1);
    }

    private List<(string HttpMethod, string ActionName, string RelativePath)> GetActions(Type controllerType)
    {
        var provider = Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();

        return provider.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .Where(d => d.ActionDescriptor is ControllerActionDescriptor cad && cad.ControllerTypeInfo.AsType() == controllerType)
            .Select(d => (
                d.HttpMethod,
                ((ControllerActionDescriptor)d.ActionDescriptor).ActionName,
                (d.RelativePath ?? string.Empty).Split('?')[0].TrimEnd('/')))
            .ToList();
    }
}
