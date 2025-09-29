using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.ResourceRegistry.Tests.Utils;
using Microsoft.AspNetCore.Mvc;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using System.Net.Http.Json;
using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Core.Services.Interfaces;

namespace Altinn.ResourceRegistry.Tests
{
    public class ResourceControllerTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.AddSingleton<IResourceRegistryRepository, RegisterResourceRepositoryMock>();
            services.AddSingleton<IApplications, ApplicationsClientMock>();

            base.ConfigureTestServices(services);
        }

        [Fact]
        public async Task GetResource_altinn_access_management_OK()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            ServiceResource? resource = JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

            Assert.NotNull(resource);
            Assert.Equal("altinn_access_management", resource.Identifier);
        }

        [Fact]
        public async Task GetResource_app_skd_flyttemelding_OK()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/app_skd_flyttemelding";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            ServiceResource? resource = JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

            Assert.NotNull(resource);
            Assert.Equal("app_skd_flyttemelding", resource.Identifier);
        }

        [Fact]
        public async Task GetResource_app_nav_flyttemelding_NotFound()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/app_nav_flyttemelding";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Test_Nav_Get()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/nav_tiltakAvtaleOmArbeidstrening";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            ServiceResource? resource = JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

            Assert.NotNull(resource);
            Assert.NotNull(resource.Identifier);
            Assert.Equal("nav_tiltakAvtaleOmArbeidstrening", resource.Identifier);
        }

        [Fact]
        public async Task Search_Get()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/Search?Id=altinn";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(3, resource.Count);
        }

        [Fact]
        public async Task ResourceList()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/resourcelist";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(441, resource.Count);

            ServiceResource? altinn2resourcewithdescription = resource.FirstOrDefault(r => r.ResourceReferences != null && r.ResourceReferences.Any(r => r.Reference != null && r.Reference.Contains("5563")));
            Assert.NotNull(altinn2resourcewithdescription);
            Assert.NotNull(altinn2resourcewithdescription.RightDescription);
            Assert.Equal("NB:Denne tjenesten er EKTJ tjeneste og betyr at man kan bare delegere rettigheter via enkeltrettigheter.\r\nKan ikke delegeres rettigheter via roller", altinn2resourcewithdescription.RightDescription["nb"]);
            Assert.Equal("EN:Denne tjenesten er EKTJ tjeneste og betyr at man kan bare delegere rettigheter via enkeltrettigheter.\r\nKan ikke delegeres rettigheter via roller", altinn2resourcewithdescription.RightDescription["en"]);
            Assert.Equal("NN:Denne tjenesten er EKTJ tjeneste og betyr at man kan bare delegere rettigheter via enkeltrettigheter.\r\nKan ikke delegeres rettigheter via roller", altinn2resourcewithdescription.RightDescription["nn"]);
        }

        [Fact]
        public async Task ResourceList_NoApps()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/resourcelist?includeApps=false";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(314, resource.Count);
        }

        [Fact]
        public async Task ResourceList_NoAltinn2()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/resourcelist?includeAltinn2=false";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(132, resource.Count);
        }

        [Fact]
        public async Task ResourceList_NoAltinn2AndNoApps()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/Resource/resourcelist?includeAltinn2=false&includeApps=false";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(5, resource.Count);
        }

        [Fact]
        public async Task CreateResource_WithErrors()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
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

            string responseContent = await response.Content.ReadAsStringAsync();

            ValidationProblemDetails? errordetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ValidationProblemDetails;

            Assert.NotNull(errordetails);

            Assert.Equal(3, errordetails.Errors.Count);
        }

        [Fact]
        public async Task CreateResource_WithAdminScope()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "altinn:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "digdir:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "difi:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "krr:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "test:TestScope"
                    },
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

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_WithValidPrefix()
        {
            var client = CreateClient();
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest"};
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "altinn:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "digdir:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "difi:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "krr:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "test:TestScope"
                    },
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

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_With_APPrefixWithoutRequiredResourceReference()
        {
            var client = CreateClient();
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "app_superdupertjenestene",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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
        }

        [Fact]
        public async Task CreateResource_With_APPrefixWithRequiredResourceReference()
        {
            var client = CreateClient();
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "app_superdupertjenestene",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn3,
                        ReferenceType = ReferenceType.ApplicationId,
                        Reference = "skd/asd"
                    },
                   
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

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateMaskinportenSchemaResource_WithoutScope_BadRequest()
        {
            var client = CreateClient();
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "schema_test",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.MaskinportenSchema
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
            Assert.Contains("Missing maskinporten scopen for MaskinportenSchema resource", content);
        }


        [Fact]
        public async Task CreateMaskinportenSchemaResource_WitScope_Ceated()
        {
            var client = CreateClient();
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "schema_test",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.MaskinportenSchema,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "altinn:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "digdir:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "difi:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "krr:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "test:TestScope"
                    },
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

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_WithInvalidPrefix()
        {
            var client = CreateClient();
            string[] prefixes = {"altinn", "digdir"};
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf  = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "altinn:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "digdir:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "difi:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "krr:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "test:TestScope"
                    },
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

            string responseContent = await response.Content.ReadAsStringAsync();

            ValidationProblemDetails? errordetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotNull(errordetails);

            Assert.Single(errordetails.Errors);
            Assert.Equal(3, errordetails.Errors["InvalidPrefix"].Length);
        }

        [Fact]
        public async Task UpdateResource_WithAdminScope()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "altinn:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "digdir:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "difi:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "krr:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "test:TestScope"
                    },
                }

            };

            string requestUri = $"resourceregistry/api/v1/Resource/{resource.Identifier}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResource_WithValidPrefix()
        {
            var client = CreateClient();
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "altinn:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "digdir:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "difi:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "krr:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "test:TestScope"
                    },
                }

            };

            string requestUri = $"resourceregistry/api/v1/Resource/{resource.Identifier}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResource_WithInvalidPrefix()
        {
            var client = CreateClient();
            string[] prefixes = { "altinn", "digdir" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource,
                ResourceReferences = new List<ResourceReference>
                {
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "altinn:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "digdir:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "difi:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "krr:TestScope"
                    },
                    new()
                    {
                        ReferenceSource = ReferenceSource.Altinn2,
                        ReferenceType = ReferenceType.MaskinportenScope,
                        Reference = "test:TestScope"
                    },
                }

            };

            string requestUri = $"resourceregistry/api/v1/Resource/{resource.Identifier}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            ValidationProblemDetails? errordetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotNull(errordetails);

            Assert.Single(errordetails.Errors);
            Assert.Equal(3, errordetails.Errors["InvalidPrefix"].Length);
        }
        
        [Fact]
        public async Task SetResourcePolicy_OK()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource() 
            { 
                Identifier = "altinn_access_management"
            };
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
        }

        [Fact]
        public async Task SetResourcePolicy_Invalid_UnknownResource()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            // unknown_resource as id in uri
            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/unknown_resource/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Post, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Unknown resource", responseContent.Replace('"', ' ').Trim());
        }

        [Fact]
        public async Task GetResourcePolicy_OK()
        {
            var client = CreateClient();
            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Get, RequestUri = requestUri };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_OK()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Put, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_InvalidResourceId()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}_invalid_resourceid.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Put, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("\"Policy not accepted: Contains rules for a different registry resource\"", responseContent);
        }

        [Fact]
        public async Task UpdateResourcePolicy_MissingResourceId()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}_invalid_missing_resourceid.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Put, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Policy not accepted: Contains rule without reference to registry resource id", responseContent.Replace('"', ' ').Trim());
        }

        [Fact]
        public async Task UpdateResource_Ok()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
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
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResource_MissingTitleNynorsk()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", " " }, { "nb", "" } },
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

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content  = await response.Content.ReadAsStringAsync();
            Assert.Contains("Missing title in nynorsk", content);
            Assert.Contains("Missing title in english", content);
            Assert.Contains("Missing title in bokmal", content);
        }

        [Fact]
        public async Task UpdateResource_MissingDescriptionBokmal()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Description = new Dictionary<string, string> { { "en", "English" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Completed",
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                }
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Missing Description in bokmal nb", content);
        }


        [Fact]
        public async Task UpdateResource_EmptyDescriptionBokmal()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Description = new Dictionary<string, string> { { "en", "" }, { "nb", " " } },
                RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Completed",
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                }
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Missing Description in bokmal nb", content);
            Assert.Contains("Missing Description in english en", content);
            Assert.Contains("Missing Description in nynorsk nn", content);
        }

        [Fact]
        public async Task UpdateResource_MissingRightsDecriptionEngelsk()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "nb", "" }, { "nn", " " } },
                Status = "Completed",
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                }
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Missing RightDescription in english en", content);
            Assert.Contains("Missing RightDescription in nynorsk nn", content);
            Assert.Contains("Missing RightDescription in bokmal nb", content);
        }

        [Fact]
        public async Task UpdateResource_MissingRightsDecriptionEngelskButNotDelegatable()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                RightDescription = new Dictionary<string, string> { { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
                Status = "Completed",
                Delegable = false,
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
                ResourceType = ResourceType.GenericAccessResource
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact]
        public async Task UpdateResource_BadRequest()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource() { Identifier = "wrong_non_matcing_id" };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        public async Task CreateResource_UnAuthorized()
        {
            var client = CreateClient();
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string>() { { "en", "Super Duper Tjenestene" } },
                Description = new Dictionary<string, string>() { { "nb", "Dette er en super duper tjeneste" } },
                RightDescription = new Dictionary<string, string> { { "nb", "Dette gir mottakere rett til super duper tjeenste" } },
                Status = "Completed",
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

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_OK_AdminScope_NotResourceOwner()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management_skd" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Put, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_Forbidden_NotResourceOwner()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("ttd", "991825888", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management_skd" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Put, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_UnAuthorized()
        {
            var client = CreateClient();
            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Put, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Deletes a resource that user is authorized for
        /// Expected result: Return httpStatus No content statuscode
        /// </summary>
        [Fact]
        public async Task Delete_AuthorizedUser_ValidResource_ReturnsNoContent()
        {
            var client = CreateClient();
            // Arrange            
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);            

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Deletes a resource that user is authorized for
        /// Expected result: Return httpStatus nocontent statuscode
        /// </summary>
        [Fact]
        public async Task Delete_AdminScope_ValidResource_ReturnsNoContent()
        {
            // Arrange
            var client = CreateClient();

            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }


        /// <summary>
        /// Deletes a resource that user is authorized for
        /// Expected result: Return httpStatus unauthorized statuscode
        /// </summary>
        [Fact]
        public async Task Delete_UnAuthorized_ValidResource_ReturnsUnauthorized()
        {
            var client = CreateClient();
            // Arrange
            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Deletes a resource that user is authorized for
        /// Expected result: Return httpStatus forbidden statuscode
        /// </summary>
        [Fact]
        public async Task Delete_Forbidden_ValidResource_ReturnsForbidden()
        {
            var client = CreateClient();
            // Arrange            
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            // Act
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Export_OK()
        {
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
        }

        [Fact]
        public async Task GetSubjectsForResource()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string requestUri = "resourceregistry/api/v1/resource/skd_mva/policy/subjects";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            Paginated<AttributeMatchV2>? subjectResources = await response.Content.ReadFromJsonAsync<Paginated<AttributeMatchV2>>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetResourceForSubjects()
        {
            var client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<string> subjects = new List<string>();
            subjects.Add("urn:altinn:rolecode:utinn");

            string requestUri = "resourceregistry/api/v1/resource/bysubjects/";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(subjects), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            Paginated<SubjectResources>? subjectResources = await response.Content.ReadFromJsonAsync<Paginated<SubjectResources>>();
           
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(subjectResources);
            Assert.NotNull(subjectResources.Items.FirstOrDefault(r => r.Subject.Urn.Contains("utinn")));
        }

        [Fact]
        public async Task GetUpdatedResourceSubjects_WithoutParameters()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/resource/updated/";

            HttpResponseMessage response = await client.GetAsync(requestUri);
            Paginated<UpdatedResourceSubject>? subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(subjectResources);
            Assert.NotNull(subjectResources.Items.FirstOrDefault(r => r.ResourceUrn.ToString().Contains("first")));
        }

        [Fact]
        public async Task GetUpdatedResourceSubjects_HasNextLink()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/resource/updated/?limit=2";

            HttpResponseMessage response = await client.GetAsync(requestUri);
            Paginated<UpdatedResourceSubject>? subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(subjectResources);
            Assert.NotNull(subjectResources.Items.FirstOrDefault(r => r.ResourceUrn.ToString().Contains("altinn")));
            Assert.NotNull(subjectResources.Links.Next);
            var token = Opaque.Create(new UpdatedResourceSubjectsContinuationToken(subjectResources.Items.Last().ResourceUrn, subjectResources.Items.Last().SubjectUrn));
            Assert.Contains($"?since=2024-02-01T00%3A00%3A00.0000000%2B00%3A00&token={token}&limit=2", subjectResources.Links.Next);
        }

        [Fact]
        public async Task GetUpdatedResourceSubjects_WithSkipPast()
        {
            var client = CreateClient();
            var token = Opaque.Create(new UpdatedResourceSubjectsContinuationToken(new Uri("urn:altinn:resource:second"), new Uri("urn:altinn:rolecode:foobar")));
            string requestUri = $"resourceregistry/api/v1/resource/updated/?Since=2024-02-01T00:00:00.0000000%2B00:00&token={token}&limit=2";

            HttpResponseMessage response = await client.GetAsync(requestUri);
            Paginated<UpdatedResourceSubject>? subjectResources = await response.Content.ReadFromJsonAsync<Paginated<UpdatedResourceSubject>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(subjectResources);
        }

        [Fact]
        public async Task GetUpdatedResourceSubjects_WithInvalidLimit()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/resource/updated/?limit=100000";

            HttpResponseMessage response = await client.GetAsync(requestUri);
            ValidationProblemDetails? errordetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(errordetails);
        }

        [Fact]
        public async Task GetUpdatedResourceSubjects_WithInvalidDateTime()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/resource/updated/?since=xxx";

            HttpResponseMessage response = await client.GetAsync(requestUri);
            ValidationProblemDetails? errordetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(errordetails);

            Assert.Single(errordetails.Errors);
            Assert.NotNull(errordetails.Errors["since"]);
        }

        [Fact]
        public async Task GetUpdatedResourceSubjects_WithInvalidSkipPast()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/resource/updated/?token=xxx";

            HttpResponseMessage response = await client.GetAsync(requestUri);
            ValidationProblemDetails? errordetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(errordetails);
        }
    }
}
