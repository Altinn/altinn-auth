using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using AngleSharp.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VDS.RDF;

namespace Altinn.ResourceRegistry.Tests;

public class ResourceControllerWithDbTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
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
        // Add one that should be marked as deleted when updating with policy
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:altinn_access_management", ["urn:altinn:rolecode:tobedeleted"], "skd"));

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

        // ensure we don't get the deleted subject
        Assert.Single(subjectMatch.Items);
        Assert.Equal("admai", subjectMatch.Items.First().Value);

    }

    [Fact]
    public async Task GetUpdatedResourceSubjects_Paginates()
    {
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:foo", ["urn:altinn:rolecode:r001", "urn:altinn:rolecode:r002"], "ttd"));

        using var client = CreateClient();
        string requestUri = "resourceregistry/api/v1/resource/updated/?limit=1";

        HttpResponseMessage response = await client.GetAsync(requestUri);
        Paginated<UpdatedResourceSubject>? subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

        Assert.NotNull(subjectResources);
        Assert.Single(subjectResources.Items);
        Assert.NotNull(subjectResources.Links.Next);
        Assert.Contains("?since=20", subjectResources.Links.Next);
        var token = Opaque.Create(new UpdatedResourceSubjectsContinuationToken(subjectResources.Items.Last().ResourceUrn, subjectResources.Items.Last().SubjectUrn));
        Assert.Contains($"&token={token}&limit=1", subjectResources.Links.Next);

        Assert.True(Uri.TryCreate(subjectResources.Links.Next, UriKind.Absolute, out Uri? nextUri));
        Assert.NotNull(nextUri);
        response = await client.GetAsync(nextUri.PathAndQuery);
        subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

        Assert.NotNull(subjectResources);
        Assert.Single(subjectResources.Items);
        Assert.Equal("urn:altinn:rolecode:r002", subjectResources.Items.First().SubjectUrn.ToString());
        Assert.Null(subjectResources.Links.Next);
    }

    [Fact]
    public async Task SetResourceSubjects_OK()
    {
        using var client = CreateClient();
        string requestUri = "resourceregistry/api/v1/resource/updated/";
        HttpResponseMessage response;
        Paginated<UpdatedResourceSubject>? subjectResources;

        UpdatedResourceSubject Subject(string roleCode)
        {
            return subjectResources.Items.Single(x => x.SubjectUrn.ToString() == $"urn:altinn:rolecode:{roleCode}");
        }

        DateTimeOffset UpdatedAtFor(string roleCode)
        {
            return Subject(roleCode)!.UpdatedAt;
        }

        // 1: First add some resources
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:foo", ["urn:altinn:rolecode:r001", "urn:altinn:rolecode:r002", "urn:altinn:rolecode:r003"], "ttd"));

        response = await client.GetAsync(requestUri);
        subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

        // Check that all pairs are returned, each with a updatedAt timestamp and deleted = false
        Assert.NotNull(subjectResources);
        Assert.Equal(3, subjectResources.Items.Count());
        Assert.True(subjectResources.Items.All(x => x.UpdatedAt > DateTimeOffset.MinValue));
        Assert.True(subjectResources.Items.All(x => x.Deleted == false));
        var role001Timestamp = UpdatedAtFor("r001");
        var role002Timestamp = UpdatedAtFor("r002");
        var role003Timestamp = UpdatedAtFor("r003");

        // 2: Now update the resource to delete subject r002, and add subject r004
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:foo", ["urn:altinn:rolecode:r001", "urn:altinn:rolecode:r003", "urn:altinn:rolecode:r004"], "ttd"));

        response = await client.GetAsync(requestUri);
        subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

        // There should be four pairs, but the item with rolecode:r002 should be marked as deleted with a higher timestamp. r001 and r003 should have the same timestamp as before
        // r004 should have the same timestamp as r002
        Assert.NotNull(subjectResources);
        Assert.Equal(4, subjectResources.Items.Count());
        Assert.NotNull(subjectResources.Items.SingleOrDefault(x => x.SubjectUrn.ToString() == "urn:altinn:rolecode:r002" && x.Deleted));
        Assert.True(role001Timestamp == UpdatedAtFor("r001"));
        Assert.True(role002Timestamp < UpdatedAtFor("r002"));
        Assert.True(role003Timestamp == UpdatedAtFor("r003"));
        Assert.True(UpdatedAtFor("r002") == UpdatedAtFor("r004"));
        role002Timestamp = UpdatedAtFor("r002");

        // 3: Now update the resource to have no subjects
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:foo", [], "ttd"));

        response = await client.GetAsync(requestUri);
        subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

        // There should be four pairs, all marked as deleted. r001, r003 and r004 should have new, identical timestamps. r002, which was already deleted, should have the same timestamp as before
        Assert.NotNull(subjectResources);
        Assert.Equal(4, subjectResources.Items.Count());
        Assert.True(subjectResources.Items.All(x => x.Deleted));
        Assert.True(role001Timestamp < UpdatedAtFor("r001"));
        Assert.True(role002Timestamp == UpdatedAtFor("r002"));
        Assert.True(UpdatedAtFor("r001") == UpdatedAtFor("r003") &&  UpdatedAtFor("r003") == UpdatedAtFor("r004"));

        // 4. Reenable the resource with r001 and r003
        await Repository.SetResourceSubjects(CreateResourceSubjects("urn:altinn:resource:foo", ["urn:altinn:rolecode:r001", "urn:altinn:rolecode:r003"], "ttd"));

        response = await client.GetAsync(requestUri);
        subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

        // There should be four pairs, but r001 and r003 should no longer be marked as deleted. They should have new, identical timestamps
        Assert.NotNull(subjectResources);
        Assert.Equal(4, subjectResources.Items.Count());
        Assert.True(!Subject("r001").Deleted && !Subject("r003").Deleted);
        Assert.True(UpdatedAtFor("r001") == UpdatedAtFor("r003"));
        Assert.True(UpdatedAtFor("r001") > UpdatedAtFor("r004"));
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
        Assert.Equal(13, subjectMatch.Items.Count());
    }

    /// <summary>
    /// Scenario: Get Policy rules for RRH innrapportering
    /// </summary>
    [Fact]
    public async Task GetpolicyRulesRRH()
    {
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v1/resource/app_brg_rrh-innrapportering/policy/rules";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        string content = await response.Content.ReadAsStringAsync();
        List<PolicyRule>? subjectMatch = await response.Content.ReadFromJsonAsync<List<PolicyRule>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(subjectMatch);
        Assert.Equal(238, subjectMatch.Count());
    }

    /// <summary>
    /// Scenario: Get Policy rules for RRH innrapportering
    /// </summary>
    [Fact]
    public async Task GetpolicyRightsRRH()
    {
        using var client = CreateAuthenticatedClient();

        string requestUri = "resourceregistry/api/v1/resource/app_brg_rrh-innrapportering/policy/rights";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        string content = await response.Content.ReadAsStringAsync();
        List<PolicyRight>? policyRights = await response.Content.ReadFromJsonAsync<List<PolicyRight>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(policyRights);
        Assert.Equal(18, policyRights.Count());
        Assert.Equal(2, policyRights[0].SubjectTypes.Count());
        Assert.Equal("urn:altinn:org", policyRights[0].SubjectTypes.ToList()[0]);
        Assert.Equal("urn:altinn:rolecode", policyRights[0].SubjectTypes.ToList()[1]);
        Assert.Equal("instantiate;rrh-innrapportering;brg;c9e4c013f36877a54c6d92bab8dcb69c", policyRights[0].RightKey);
        Assert.Equal(2, policyRights[1].SubjectTypes.Count());
        Assert.Equal("urn:altinn:org", policyRights[1].SubjectTypes.ToList()[0]);
        Assert.Equal("urn:altinn:rolecode", policyRights[1].SubjectTypes.ToList()[1]);
        Assert.Equal("read;rrh-innrapportering;brg;52a1e8911c3a9d3a7d2b837ae29a0dd8", policyRights[1].RightKey);
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
        Assert.Equal(13, subjectMatch.Items.Count());
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

    [Fact]
    public async Task CreateResource_Ok()
    {
        var client = CreateClient();
        string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        ServiceResource resource = new ServiceResource()
        {
            Identifier = "superdupertjenestene",
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Completed",
            ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
            HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "skd",
            },
            ResourceType = ResourceType.GenericAccessResource,
        };

        string requestUri = "resourceregistry/api/v1/Resource/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SearchResources_Ok()
    {
        await LoadTestData();

        var client = CreateClient();
        string requestUri = "resourceregistry/api/v1/Resource/Search?Id=korrespondanse-fra-sivilforsvaret";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        string responseContent = await response.Content.ReadAsStringAsync();
        List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

        Assert.NotNull(resource);
        Assert.Single(resource);
    }

    /// <summary>
    /// Scenario: Search for resources by ServiceEditionVersion reference
    /// This is relevant when migrating consents from Altinn 2 to Altinn 3 where the consent is tied to a specific version of a service edition
    /// The goal is to find the correct 
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SearchResources_ServiceEditionVersion_Ok()
    {
        await LoadTestDataWithUpdates();

        var client = CreateClient();
        string requestUri = "resourceregistry/api/v1/Resource/Search?reference=7846";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        string responseContent = await response.Content.ReadAsStringAsync();
        List<ServiceResource>? matchingResources = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

        Assert.NotNull(matchingResources);
        Assert.Single(matchingResources);

        Assert.Equal("skd-migrert-4628-1", matchingResources[0].Identifier);

        ServiceResource resource = matchingResources[0];

        Assert.NotNull(resource.ResourceReferences);
        Assert.Equal(3, resource.ResourceReferences.Count);
        Assert.Contains(resource.ResourceReferences, r => r.ReferenceType == ReferenceType.ServiceEditionVersion && r.Reference == "7846");
        Assert.True(resource.VersionId > 1, "Expected resource to be an updated version");

        string requestUriAllResources = "resourceregistry/api/v1/Resource/Search";
        HttpRequestMessage httpRequestMessageAllResources = new HttpRequestMessage(HttpMethod.Get, requestUriAllResources)
        {
        };
        HttpResponseMessage responseAllResources = await client.SendAsync(httpRequestMessageAllResources);
        string responseContentAllResources = await responseAllResources.Content.ReadAsStringAsync();
        List<ServiceResource>? allResources = JsonSerializer.Deserialize<List<ServiceResource>>(responseContentAllResources, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

        Assert.NotNull(allResources);
        Assert.True(allResources.Count > 1, "Expected multiple resources in test data");
      
        Assert.DoesNotContain(allResources, r => r.Identifier == "skd-migrert-4628-1" && r.VersionId == resource.VersionId);
        Assert.Contains(allResources, r => r.Identifier == "skd-migrert-4628-1" && r.VersionId > resource.VersionId);
    }

    [Fact]
    public async Task GetResource_skd_migrert_4628_1_OK()
    {
        await LoadTestDataWithUpdates();
        var client = CreateClient();
        string requestUriSearch = "resourceregistry/api/v1/Resource/Search?reference=7846";

        HttpRequestMessage httpRequestMessageSearch = new HttpRequestMessage(HttpMethod.Get, requestUriSearch)
        {
        };

        HttpResponseMessage responseSearch = await client.SendAsync(httpRequestMessageSearch);

        string responseContentSearch = await responseSearch.Content.ReadAsStringAsync();
        List<ServiceResource>? matchingResources = JsonSerializer.Deserialize<List<ServiceResource>>(responseContentSearch, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

        Assert.NotNull(matchingResources);
        Assert.Single(matchingResources);
        Assert.Equal("skd-migrert-4628-1", matchingResources[0].Identifier);

        ServiceResource oldVersion = matchingResources[0];

        string requestUri = "resourceregistry/api/v1/Resource/skd-migrert-4628-1";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        string responseContent = await response.Content.ReadAsStringAsync();
        ServiceResource? resource = JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

        Assert.NotNull(resource);
        Assert.Equal("skd-migrert-4628-1", resource.Identifier);
        Assert.True(resource.VersionId > oldVersion.VersionId, "Expected resource to be an updated version");

        // Try getting the resource again but now with the version id to get the old version

        string requestUriWithVersion = $"resourceregistry/api/v1/Resource/skd-migrert-4628-1?versionId={oldVersion.VersionId}";
        HttpRequestMessage httpRequestMessageWithVersion = new HttpRequestMessage(HttpMethod.Get, requestUriWithVersion)
        {
        };
        HttpResponseMessage responseWithVersion = await client.SendAsync(httpRequestMessageWithVersion);
        string responseContentWithVersion = await responseWithVersion.Content.ReadAsStringAsync();
        ServiceResource? resourceWithVersion = JsonSerializer.Deserialize<ServiceResource>(responseContentWithVersion, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;  
        Assert.NotNull(resourceWithVersion);
        Assert.Equal("skd-migrert-4628-1", resourceWithVersion.Identifier);
        Assert.Equal(oldVersion.VersionId, resourceWithVersion.VersionId);

        // Try getting a non existing version
        string requestUriWithNonExistingVersion = $"resourceregistry/api/v1/Resource/skd-migrert-4628-1?versionId=9999";
        HttpRequestMessage httpRequestMessageWithNonExistingVersion = new HttpRequestMessage(HttpMethod.Get, requestUriWithNonExistingVersion)
        {
        };
        HttpResponseMessage responseWithNonExistingVersion = await client.SendAsync(httpRequestMessageWithNonExistingVersion);
        Assert.Equal(HttpStatusCode.NotFound, responseWithNonExistingVersion.StatusCode);
    }

    /// <summary>
    /// Scenario: Update existing resource
    /// Expects: 200 OK and version incremented
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateResource_Ok()
    {
        var client = CreateClient();
        string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        ServiceResource resource = new ServiceResource()
        {
            Identifier = "superdupertjenestene",
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Completed",
            ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
            HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "skd",
            },
            ResourceType = ResourceType.GenericAccessResource,
        };

        string requestUri = "resourceregistry/api/v1/Resource/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);


        string getRequestUri = "resourceregistry/api/v1/Resource/superdupertjenestene";


        HttpRequestMessage httpGetRequestMessage = new HttpRequestMessage(HttpMethod.Get, getRequestUri)
        {
        };

        HttpResponseMessage responseGet = await client.SendAsync(httpGetRequestMessage);

        Assert.Equal(HttpStatusCode.OK, responseGet.StatusCode);

        ServiceResource? createdResource = JsonSerializer.Deserialize<ServiceResource>(await responseGet.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

        Assert.NotNull(createdResource);

        ServiceResource updatedresource = new ServiceResource()
        {
            Identifier = "superdupertjenestene",
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Completed",
            ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
            HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "skd",
            },
            ResourceType = ResourceType.GenericAccessResource
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        string updateRequestUri = "resourceregistry/api/v1/Resource/superdupertjenestene";

        HttpRequestMessage httpupdateRequestMessage = new HttpRequestMessage(HttpMethod.Put, updateRequestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(updatedresource), Encoding.UTF8, "application/json")
        };

        httpupdateRequestMessage.Headers.Add("Accept", "application/json");
        httpupdateRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage responseUpdate = await client.SendAsync(httpupdateRequestMessage);

        Assert.Equal(HttpStatusCode.OK, responseUpdate.StatusCode);


        HttpRequestMessage httpGetUpdatedResource = new HttpRequestMessage(HttpMethod.Get, getRequestUri)
        {
        };

        HttpResponseMessage responseGetUpdated = await client.SendAsync(httpGetUpdatedResource);

        Assert.Equal(HttpStatusCode.OK, responseGetUpdated.StatusCode);

        ServiceResource? updatedResource = JsonSerializer.Deserialize<ServiceResource>(await responseGetUpdated.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

        Assert.NotNull(updatedResource);

        Assert.Equal(createdResource.VersionId + 1, updatedResource.VersionId);
    }

    [Fact]
    public async Task ResourceListWithMultpleVersionsReturnCorrect()
    {
        await LoadTestDataWithUpdates();
        var client = CreateClient();
        string requestUri = "resourceregistry/api/v1/Resource/resourcelist?includeAltinn2=false&includeApps=false";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        string responseContent = await response.Content.ReadAsStringAsync();
        List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

        Assert.NotNull(resource);
        Assert.Equal(6, resource.Count);

        ServiceResource? skd_maskinportenSchema = resource.FirstOrDefault(r => r.ResourceReferences != null && r.ResourceReferences.Any(r => r.Reference != null && r.Reference.Contains("folkeregister:deling/finans")));
        Assert.NotNull(skd_maskinportenSchema);
        Assert.NotNull(skd_maskinportenSchema.RightDescription);
        Assert.Equal("This service allows you to delegate your access to The national Population Register information to a provider. Once the delegation has been completed, the provider will be notified that they can use the services available within the rights", skd_maskinportenSchema.RightDescription["en"]);
        Assert.Equal("Denne tjenesten gir deg mulighet for å delegere din tilgang til folkeregisteropplysninger til en  leverandør. Når delegeringen er utført, vil leverandøren motta melding om at de på vegne av din virksomhet kan benyttet de tjenester som er ti", skd_maskinportenSchema.RightDescription["nb"]);
        Assert.Equal("Denne tenesta gir deg moglegheit for å delegera tilgangen din til folkeregisteropplysningar til ein leverandør. Når delegeringen er utførte, vil leverandøren få melding om at dei på vegner av verksemda di kan nytta dei tenestene som er tilg", skd_maskinportenSchema.RightDescription["nn"]);
        Assert.True(skd_maskinportenSchema.VersionId > 4);
    }

    /// <summary>
    /// ID Contains caps A
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateResource_InvalidId()
    {
        var client = CreateClient();
        string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        ServiceResource resource = new ServiceResource()
        {
            Identifier = "Asuperdupertjenestene",
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Completed",
            ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
            HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "skd",
            },
            ResourceType = ResourceType.GenericAccessResource
        };

        string requestUri = "resourceregistry/api/v1/Resource/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid id. Only a-z and 0-9 is allowed", content);
    }

    [Fact]
    public async Task CreateResource_ThoShortIdId()
    {
        var client = CreateClient();
        string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        ServiceResource resource = new ServiceResource()
        {
            Identifier = "a12",
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Completed",
            ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
            HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "skd",
            }
        };

        string requestUri = "resourceregistry/api/v1/Resource/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid id. Only a-z and 0-9 is allowed", content);
    }

    [Fact]
    public async Task CreateResource_Forbidden_NotResourceOwner()
    {
        var client = CreateClient();
        string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        ServiceResource resource = new ServiceResource()
        {
            Identifier = "superdupertjenestene",
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Completed",
            ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
            HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
            {
                Organization = "991825827",
                Orgcode = "digdir",
            },
            ResourceType = ResourceType.GenericAccessResource
        };

        string requestUri = "resourceregistry/api/v1/Resource/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateResource_AdminScope_OK_NotResourceOwner()
    {
        var client = CreateClient();
        string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        ServiceResource resource = new ServiceResource()
        {
            Identifier = "superdupertjenestene",
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Completed",
            ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
            HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
            {
                Organization = "974761076",
                Orgcode = "skd",
            },
            ResourceType = ResourceType.GenericAccessResource
        };

        string requestUri = "resourceregistry/api/v1/Resource/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Export_OK()
    {
        await LoadTestDataWithUpdates();
        var client = CreateClient();
        string requestUri = "resourceregistry/api/v1/Resource/export";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
        };

        httpRequestMessage.Headers.Add("Accept", "application/text");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

        Assert.True(response.IsSuccessStatusCode);
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotNull(responseContent);
        Assert.Contains("Skattegrunnlag latest version", responseContent);
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
        if(version7658 != null)
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
