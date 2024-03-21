using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class ResourceControllerWithDbTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ORG_NR = "974761076";

    protected IResourceRegistryRepository Repository => Services.GetRequiredService<IResourceRegistryRepository>();
    protected AdvanceableTimeProvider TimeProvider => Services.GetRequiredService<AdvanceableTimeProvider>();
    protected NpgsqlDataSource DataSource => Services.GetRequiredService<NpgsqlDataSource>();


    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_WRITE);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Scenario: Two different resources is registrated on two different roles.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetResourceForSubjects()
    {
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:skd_mva", ["urn:altinn:rolecode:utinn"], "skd"));
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:skd_flyttemelding", ["urn:altinn:rolecode:utinn", "urn:altinn:rolecode:dagl" ], "skd"));

        using var client = CreateAuthenticatedClient();

        List<string> subjects = new List<string>();
        subjects.Add("urn:altinn:rolecode:utinn");
        subjects.Add("urn:altinn:rolecode:dagl");

        string requestUri = "resourceregistry/api/v1/resource/bysubjects/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(subjects), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Paginated<SubjectResources>? subjectResourcesPaginated = await response.Content.ReadFromJsonAsync<Paginated<SubjectResources>>();
        Assert.NotNull(subjectResourcesPaginated);
        List<SubjectResources> subjectResources = subjectResourcesPaginated.Items.ToList();

        Assert.Equal(2, subjectResources.Count);
        Assert.Equal(2, subjectResources[0].Resources.Count);
        Assert.Single(subjectResources[1].Resources);
        Assert.NotNull(subjectResources.FirstOrDefault(r => r.Subject.Urn.Contains("utinn")));
    }

    /// <summary>
    /// Scenario: Two different resources is registrated on two different roles.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetSubjectsForPolicy()
    {
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:skd_mva", ["urn:altinn:rolecode:utinn"], "skd"));
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:skd_flyttemelding", [ "urn:altinn:rolecode:utinn", "urn:altinn:rolecode:dagl" ], "skd"));

        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v1/resource/skd_mva/policy/subjects";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        Paginated<AttributeMatchV2>? subjectMatch = await response.Content.ReadFromJsonAsync<Paginated<AttributeMatchV2>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(subjectMatch);
    }

    [Fact]
    public async Task SetResourcePolicy_OK()
    {
        ServiceResource resource = new ServiceResource()
        {
            Identifier = "altinn_access_management",
            HasCompetentAuthority = new CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "skd"
            }
        };
        await Repository.CreateResource(resource);

        using var client = CreateClient();
        string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    
        string fileName = $"{resource.Identifier}.xml";
        string filePath = $"Data/ResourcePolicies/{fileName}";

        Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

        ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

        MultipartFormDataContent content = new();
        content.Add(fileContent, "policyFile", fileName);

        HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Post, RequestUri = requestUri, Content = content };
        httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        requestUri = new Uri("resourceregistry/api/v1/resource/altinn_access_management/policy/subjects", UriKind.Relative);

        httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response2 = await client.SendAsync(httpRequestMessage);
        Paginated<AttributeMatchV2>? subjectMatch = await response2.Content.ReadFromJsonAsync<Paginated<AttributeMatchV2>>();

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(subjectMatch);
    }

    /// <summary>
    /// Scenario: Reload subject resources for rrh-innlevering. App not imported to registry. Expects 12 subjects
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetSubjectsForAppPolicyWithReload()
    {
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v1/resource/app_brg_rrh-innrapportering/policy/subjects?reloadFromXacml=true";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        Paginated<AttributeMatchV2>? subjectMatch = await response.Content.ReadFromJsonAsync<Paginated<AttributeMatchV2>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(subjectMatch);
        Assert.Equal(12, subjectMatch.Items.Count());
    }


    /// <summary>
    /// Scenario: Reload subject resources for rrh-innlevering. App os imported to registry. Expects 12 subjects
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetSubjectsForImportedAppPolicyWithReload()
    {
        ServiceResource resource = new ServiceResource()
        {
            Identifier = "app_brg_rrh-innrapportering",
            HasCompetentAuthority = new CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "brg"
            }
        };

        await Repository.CreateResource(resource);

        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v1/resource/app_brg_rrh-innrapportering/policy/subjects?reloadFromXacml=true";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        Paginated<AttributeMatchV2>? subjectMatch = await response.Content.ReadFromJsonAsync<Paginated<AttributeMatchV2>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(subjectMatch);
        Assert.Equal(12, subjectMatch.Items.Count());
    }



    /// <summary>
    /// Scenario: Reload subject resources for rrh-innlevering. Expects 12 subjects
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetSubjectsForResourcePolicyWithReload()
    {
        ServiceResource resource = new ServiceResource()
        {
            Identifier = "altinn_access_management",
            HasCompetentAuthority = new CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "digdir"
            }
        };

        await Repository.CreateResource(resource);
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v1/resource/altinn_access_management/policy/subjects?reloadFromXacml=true";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        Paginated<AttributeMatchV2>? subjectMatch = await response.Content.ReadFromJsonAsync<Paginated<AttributeMatchV2>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(subjectMatch);
        Assert.Single(subjectMatch.Items);
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
    #endregion
}
