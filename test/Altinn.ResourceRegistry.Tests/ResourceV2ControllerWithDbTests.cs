using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Altinn.ResourceRegistry.Tests;

public class ResourceV2ControllerWithDbTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ORG_NR = "974761076";

    protected IResourceRegistryRepository Repository => Services.GetRequiredService<IResourceRegistryRepository>();
    protected AdvanceableTimeProvider TimeProvider => Services.GetRequiredService<AdvanceableTimeProvider>();


    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_WRITE);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Scenario: Get Policy rules for RRH innrapportering
    /// </summary>
    [Fact]
    public async Task GetpolicyRightsRRH()
    {
        await LoadTestDataWithUpdates();
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v2/resource/app_brg_rrh-innrapportering/policy/rights";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ResourceDecomposedDto? policyRights = await response.Content.ReadFromJsonAsync<ResourceDecomposedDto>();
        Assert.NotNull(policyRights);
        Assert.NotEmpty(policyRights!.Rights);

        foreach(RightDecomposedDto right in policyRights.Rights)
        {
            Assert.NotNull(right.Right.Key);
            Assert.NotNull(right.Right.Name);
            Assert.Equal(right.Right.Key, right.Right.Key.ToLowerInvariant());
        }
    }

    [Fact]
    public async Task GetpolicyRightsRRHEnglish()
    {
        await LoadTestDataWithUpdates();
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v2/resource/app_brg_rrh-innrapportering/policy/rights";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequestMessage.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ResourceDecomposedDto? policyRights = await response.Content.ReadFromJsonAsync<ResourceDecomposedDto>();
        Assert.NotNull(policyRights);
        Assert.NotEmpty(policyRights!.Rights);

        foreach (RightDecomposedDto right in policyRights.Rights)
        {
            Assert.NotNull(right.Right.Key);
            Assert.NotNull(right.Right.Name);
            Assert.Equal(right.Right.Key, right.Right.Key.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Scenario: Get Policy rules for RRH innrapportering
    /// </summary>
    [Fact]
    public async Task GetpolicyRightsTerjeApp()
    {
        await LoadTestDataWithUpdates();
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v2/resource/app_ttd_terje-sin-test-app/policy/rights";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ResourceDecomposedDto? policyRights = await response.Content.ReadFromJsonAsync<ResourceDecomposedDto>();
        Assert.NotNull(policyRights);
        Assert.NotEmpty(policyRights!.Rights);

        foreach (RightDecomposedDto right in policyRights.Rights)
        {
            Assert.NotNull(right.Right.Key);
            Assert.NotNull(right.Right.Name);
            Assert.Equal(right.Right.Key, right.Right.Key.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Scenario: Get Policy rules for Aarsregnskap Vanlig 202406
    /// </summary>
    [Fact]
    public async Task GetpolicyRightsaarsregnskapvanlig202406()
    {
        await LoadTestDataWithUpdates();
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v2/resource/app_brg_aarsregnskap-vanlig-202406/policy/rights";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ResourceDecomposedDto? policyRights = await response.Content.ReadFromJsonAsync<ResourceDecomposedDto>();
        Assert.NotNull(policyRights);
        Assert.NotEmpty(policyRights!.Rights);

        foreach (RightDecomposedDto right in policyRights.Rights)
        {
            Assert.NotNull(right.Right.Key);
            Assert.NotNull(right.Right.Name);
            Assert.Equal(right.Right.Key, right.Right.Key.ToLowerInvariant());
        }
    }


    #region Utils
    private static ResourceSubjects CreateResourceSubjects(string resourceurn, List<string> subjecturns, string owner)
    {
        AttributeMatchV2 resourceMatch = new AttributeMatchV2
        {
            Type = resourceurn.Substring(0, resourceurn.LastIndexOf(':')),
            Value = resourceurn.Substring(resourceurn.LastIndexOf(':') + 1),
            Urn = resourceurn
        };

        ResourceSubjects resourceSubjects = new ResourceSubjects
        {
            Resource = resourceMatch,
            Subjects = new List<AttributeMatchV2>(),
            ResourceOwner = owner
        };

        resourceSubjects.Subjects = new List<AttributeMatchV2>();
        foreach (string subjecturn in subjecturns)
        {
            resourceSubjects.Subjects.Add(new AttributeMatchV2
            {
                Type = subjecturn.Substring(0, subjecturn.LastIndexOf(':')),
                Value = subjecturn.Substring(subjecturn.LastIndexOf(':') + 1),
                Urn = subjecturn
            });
        }

        return resourceSubjects;
    }


    private async Task LoadTestData()
    {
        List<ServiceResource> testData = await GetTestData();
        foreach (ServiceResource resource in testData)
        {
            await Repository.CreateResource(resource);
        }
    }

    private async Task LoadTestDataWithUpdates()
    {
        List<ServiceResource> testData = await GetTestData();
        foreach (ServiceResource resource in testData)
        {
            await Repository.CreateResource(resource);
        }

        foreach (ServiceResource resource in testData)
        {
            resource.Description = new Dictionary<string, string> { { "en", "Updated English" }, { "nb", "Updated Bokmal" }, { "nn", "Updated Nynorsk" } };
            await Repository.UpdateResource(resource);
        }

        RegisterResourceRepositoryMock repositoryMock = new();
        ServiceResource? version7658 = await repositoryMock.GetResource("skd-migrert-4628-1-7846", null);
        if (version7658 != null)
        {
            await Repository.UpdateResource(version7658);
        }

        ServiceResource? version9546 = await repositoryMock.GetResource("skd-migrert-4628-1-9546", null);
        if (version9546 != null)
        {
            await Repository.UpdateResource(version9546);
        }
    }


    private async Task<List<ServiceResource>> GetTestData()
    {
        List<ServiceResource> resources = new List<ServiceResource>();

        RegisterResourceRepositoryMock repositoryMock = new RegisterResourceRepositoryMock();

        string[] testResources = GetTestServices();

        foreach (string testResource in testResources)
        {
            ServiceResource? resource = await repositoryMock.GetResource(testResource, null);
            if (resource != null)
            {
                resources.Add(resource);
            }
        }

        return resources;
    }

    private string[] GetTestServices()
    {
        return
        [
            "eformidling-dpo-meldingsutveksling",
            "korrespondanse-fra-sivilforsvaret",
            "skd-maskinportenschemaid-8",
            "ske-innrapportering-boligsameie",
            "stami-samtykke-must",
            "skd-migrert-4628-1-7381"
        ];

    }

    #endregion
}
