using Altinn.AccessManagement.TestUtils.Fixtures;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using ControllersV1 = Altinn.AccessManagement.Api.Enduser.Controllers;
using ControllersV2 = Altinn.AccessManagement.Api.Enduser.Controllers.V2;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers.V2;

/// <summary>
/// Guards that the v1 routes do not leak into the v2 Maskinporten controllers. The v2 controllers
/// inherit all actions from v1 and only override the class route, so every action on a v2 controller
/// must resolve under its own v2 route and nowhere else, and v1 must keep its own routes.
/// </summary>
[IntegrationTest]
public class MaskinportenRouteIsolationTest : IClassFixture<ApiFixture>
{
    private const string ConsumersV1Route = "accessmanagement/api/v1/enduser/maskinportenconsumers";
    private const string SuppliersV1Route = "accessmanagement/api/v1/enduser/maskinportensuppliers";
    private const string ConsumersV2Route = "accessmanagement/api/v2/enduser/maskinporten/consumers";
    private const string SuppliersV2Route = "accessmanagement/api/v2/enduser/maskinporten/suppliers";

    public MaskinportenRouteIsolationTest(ApiFixture fixture)
    {
        fixture.BuildConfiguration();
        Services = fixture.Services;
    }

    private IServiceProvider Services { get; }

    public static TheoryData<Type, string> ControllerRoutes => new()
    {
        { typeof(ControllersV1.MaskinportenConsumersController), ConsumersV1Route },
        { typeof(ControllersV1.MaskinportenSuppliersController), SuppliersV1Route },
        { typeof(ControllersV2.MaskinportenConsumersController), ConsumersV2Route },
        { typeof(ControllersV2.MaskinportenSuppliersController), SuppliersV2Route },
    };

    [Theory]
    [MemberData(nameof(ControllerRoutes))]
    public void Controller_OnlyExposesActionsUnderItsOwnRoute(Type controllerType, string expectedRoute)
    {
        var paths = GetPaths(controllerType);

        paths.Should().NotBeEmpty();
        paths.Should().OnlyContain(p => p == expectedRoute || p.StartsWith($"{expectedRoute}/", StringComparison.Ordinal));
    }
        
    private List<string> GetPaths(Type controllerType) =>
        GetActions(controllerType).Select(a => a.RelativePath).ToList();

    private List<(string HttpMethod, string ActionName, string RelativePath)> GetActions(Type controllerType)
    {
        var provider = Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();

        var result = provider.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .Where(d => d.ActionDescriptor is ControllerActionDescriptor cad && cad.ControllerTypeInfo.AsType() == controllerType)
            .Select(d => (
                d.HttpMethod,
                ((ControllerActionDescriptor)d.ActionDescriptor).ActionName,
                (d.RelativePath ?? string.Empty).Split('?')[0].TrimEnd('/')))
            .ToList();
        return result;
    }
}
