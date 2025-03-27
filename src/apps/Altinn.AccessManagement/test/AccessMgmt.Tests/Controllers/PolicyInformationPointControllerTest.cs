using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Utils;
using Altinn.Authorization.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Tests.Controllers;

/// <summary>
/// Test class for <see cref="PolicyInformationPointController"></see>
/// </summary>
public class PolicyInformationPointControllerTest : IClassFixture<CustomWebApplicationFactory<PolicyInformationPointController>>
{
    private HttpClient _client;
    private readonly CustomWebApplicationFactory<PolicyInformationPointController> _factory;
    private readonly JsonSerializerOptions options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyInformationPointControllerTest"/> class.
    /// </summary>
    /// <param name="factory">CustomWebApplicationFactory</param>
    public PolicyInformationPointControllerTest(CustomWebApplicationFactory<PolicyInformationPointController> factory)
    {
        _factory = factory;
        _client = GetTestClient();
    }

    private HttpClient GetTestClient(IDelegationMetadataRepository delegationMetadataRepositoryMock = null)
    {
        delegationMetadataRepositoryMock ??= new DelegationMetadataRepositoryMock();

        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(delegationMetadataRepositoryMock);
                services.AddSingleton<IPartiesClient, PartiesClientMock>();
                services.AddSingleton<IProfileClient, ProfileClientMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        return client;
    }

    /// <summary>
    /// Sets up test scenarios for <see cref="PolicyInformationPointController.GetAllDelegationChanges(Core.Models.DelegationChangeInput, System.Threading.CancellationToken)"></see>
    /// </summary>
    public static TheoryData<string> Scenarios() => new()
    {
        { "app_toPerson" },
        { "resource_toPerson" },
        { "app_toSystemUser" },
        { "resource_toSystemUser" }
    };

    /// <summary>
    /// Sets up test scenarios for accesspackage delegations to system users, to test <see cref="PolicyInformationPointController.GetAccessPackages(Guid, Guid, CancellationToken)"></see>
    /// </summary>
    public static TheoryData<string, string, string, List<AccessPackageUrn>> SystemUserAccessPackageScenarios() => new()
    {
        //// ToDo: Add Support for Direct delegations { "directDelgFromMainUnit_fromMainUnit_toSystemUser", "066148fe-7077-4484-b7ea-44b5ede0014e", "e2eba2c3-b369-4ff9-8418-99a810d6bb58", new List<AccessPackageUrn> { AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked("ansettelsesforhold")) } },
        //// ToDo: Add Support for Direct delegations { "directDelgFromMainUnit_fromSubUnit_toSystemUser", "825d14bf-b3f3-4d68-ae33-0994febf8a43", "e2eba2c3-b369-4ff9-8418-99a810d6bb58" , new List<AccessPackageUrn> { AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked("ansettelsesforhold")) } },
        { "clientDelgFromMainUnitClient_fromMainUnitClient_toSystemUser", "c12f8f37-391b-4651-be09-05665f5acdb6", "e2eba2c3-b369-4ff9-8418-99a810d6bb58", new List<AccessPackageUrn> { AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked("regnskapsforer-med-signeringsrettighet")) } },
        { "clientDelgFromMainUnitClient_fromSubUnitClient_toSystemUser", "86ae6d6a-3545-4956-b395-c67ca0df4e51", "e2eba2c3-b369-4ff9-8418-99a810d6bb58", new List<AccessPackageUrn> { AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked("regnskapsforer-med-signeringsrettighet")) } },
        { "clientDelgFromEnkClient_fromEnkClient_toSystemUser", "ab07bec2-fcd0-4563-908a-d9f564724252", "e2eba2c3-b369-4ff9-8418-99a810d6bb58", new List<AccessPackageUrn> { AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked("regnskapsforer-lonn")) } },
        //// ToDo: Add Support for inherited rights for Innehaver through REVI/REGN{ "clientDelgFromEnkClient_fromEnkClientInnh_toSystemUser", "00273506-3b4a-4e8e-a1f7-b7f28c4b411b", "e2eba2c3-b369-4ff9-8418-99a810d6bb58", new List<AccessPackageUrn> { AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked("regnskapsforer-lonn")) } }
    };

    /// <summary>
    /// Test case: Tests if you can get all delegation changes for a resource
    /// Expected: Returns delegation changes for a resource
    /// </summary>
    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task GetDelegationChanges_ValidResponse(string scenario)
    {
        // Act
        HttpResponseMessage actualResponse = await _client.PostAsync($"accessmanagement/api/v1/policyinformation/getdelegationchanges", GetRequest(scenario));

        // Assert
        Assert.Equal(HttpStatusCode.OK, actualResponse.StatusCode);

        List<DelegationChangeExternal> actualDelegationChanges = JsonSerializer.Deserialize<List<DelegationChangeExternal>>(await actualResponse.Content.ReadAsStringAsync(), options);
        AssertionUtil.AssertEqual(GetExpected(scenario), actualDelegationChanges);
    }

    /* ToDo: Add Integration tests on database container
    /// <summary>
    /// Test case: Tests getting all access packages av given to-party has for a given from-party
    /// Expected: Returns collection of access packages
    /// </summary>
    [Theory]
    [MemberData(nameof(SystemUserAccessPackageScenarios))]
    public async Task GetAccessPackages_ValidResponse(string scenario, string from, string to, List<AccessPackageUrn> expected)
    {
        // Act
        HttpResponseMessage actualResponse = await _client.GetAsync($"accessmanagement/api/v1/policyinformation/accesspackages?from={from}&to={to}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, actualResponse.StatusCode);

        List<AccessPackageUrn> actualAccessPackages = await actualResponse.Content.ReadFromJsonAsync<List<AccessPackageUrn>>(options);
        AssertionUtil.AssertCollections(expected, actualAccessPackages, AssertionUtil.AssertAccessPackageUrn);
    }
    */

    private static StreamContent GetRequest(string scenario)
    {
        Stream dataStream = File.OpenRead($"Data/PolicyInformationPoint/Requests/{scenario}.json");
        StreamContent content = new StreamContent(dataStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private List<DelegationChangeExternal> GetExpected(string scenario)
    {
        string expectedContent = File.ReadAllText($"Data/PolicyInformationPoint/Expected/{scenario}.json");
        return (List<DelegationChangeExternal>)JsonSerializer.Deserialize(expectedContent, typeof(List<DelegationChangeExternal>), options);
    }
}
