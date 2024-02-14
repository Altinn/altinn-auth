using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Altinn.ResourceRegistry.Tests.Utils;
using Microsoft.AspNetCore.Mvc;
using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ResourceRegistry.Tests
{
    public class ResourceControllerTest : IClassFixture<CustomWebApplicationFactory<ResourceController>>
    {
        private readonly CustomWebApplicationFactory<ResourceController> _factory;
        private readonly HttpClient _client;

        public ResourceControllerTest(CustomWebApplicationFactory<ResourceController> factory)
        {
            _factory = factory;
            _client = SetupUtil.GetTestClient(factory);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [Fact]
        public async Task GetResource_altinn_access_management_OK()
        {
            string requestUri = "resourceregistry/api/v1/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            ServiceResource? resource = JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

            Assert.NotNull(resource);
            Assert.Equal("altinn_access_management", resource.Identifier);
        }

        [Fact]
        public async Task Test_Nav_Get()
        {
            string requestUri = "resourceregistry/api/v1/Resource/nav_tiltakAvtaleOmArbeidstrening";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            ServiceResource? resource = JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

            Assert.NotNull(resource);
            Assert.NotNull(resource.Identifier);
            Assert.Equal("nav_tiltakAvtaleOmArbeidstrening", resource.Identifier);
        }

        [Fact]
        public async Task Search_Get()
        {
            string requestUri = "resourceregistry/api/v1/Resource/Search?Id=altinn";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(2, resource.Count);
        }

        [Fact]
        public async Task ResourceList()
        {
            string requestUri = "resourceregistry/api/v1/Resource/resourcelist";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(439, resource.Count);
        }

        [Fact]
        public async Task CreateResource_WithErrors()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            ValidationProblemDetails? errordetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ValidationProblemDetails;

            Assert.NotNull(errordetails);

            Assert.Equal(5, errordetails.Errors.Count);
        }

        [Fact]
        public async Task CreateResource_WithAdminScope()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_WithValidPrefix()
        {
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest"};
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_With_APPrefixWithoutRequiredResourceReference()
        {
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "app_superdupertjenestene",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_With_APPrefixWithRequiredResourceReference()
        {
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "app_superdupertjenestene",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }



        [Fact]
        public async Task CreateResource_WithInvalidPrefix()
        {
            string[] prefixes = {"altinn", "digdir"};
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf  = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            ValidationProblemDetails? errordetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotNull(errordetails);

            Assert.Equal(1, errordetails.Errors.Count);
            Assert.Equal(3, errordetails.Errors["InvalidPrefix"].Length);
        }

        [Fact]
        public async Task UpdateResource_WithAdminScope()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResource_WithValidPrefix()
        {
            string[] prefixes = { "altinn", "digdir", "difi", "krr", "test", "digdirintern", "idporten", "digitalpostinnbygger", "minid", "move", "difitest" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResource_WithInvalidPrefix()
        {
            string[] prefixes = { "altinn", "digdir" };
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write", prefixes);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string>(),
                Status = "Active",
                Homepage = "www.altinn.no",
                IsPartOf = "Altinn",
                Keywords = new List<Keyword>(),
                Description = new Dictionary<string, string>(),
                RightDescription = new Dictionary<string, string>(),
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            ValidationProblemDetails? errordetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotNull(errordetails);

            Assert.Equal(1, errordetails.Errors.Count);
            Assert.Equal(3, errordetails.Errors["InvalidPrefix"].Length);
        }
        
        [Fact]
        public async Task SetResourcePolicy_OK()
        {
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task SetResourcePolicy_Invalid_UnknownResource()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Unknown resource", responseContent.Replace('"', ' ').Trim());
        }

        [Fact]
        public async Task GetResourcePolicy_OK()
        {
            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Get, RequestUri = requestUri };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpClient client = SetupUtil.GetTestClient(_factory);
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_OK()
        {
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_InvalidResourceId()
        {
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("\"Policy not accepted: Contains rules for a different registry resource\"", responseContent);
        }

        [Fact]
        public async Task UpdateResourcePolicy_MissingResourceId()
        {
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Policy not accepted: Contains rule without reference to registry resource id", responseContent.Replace('"', ' ').Trim());
        }

        [Fact]
        public async Task UpdateResource_Ok()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource()
            {
                Identifier = "altinn_access_management",
                Title = new Dictionary<string, string>() { { "en", "Maskinporten Scope Delegation" } },
                Description = new Dictionary<string, string>() { { "nb", "Maskinporten Scope Delegation" } },
                RightDescription = new Dictionary<string, string> { { "nb", "Access to Maskinporten Scope Delegation" } },
                Status = "Completed",
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "974761076",
                    Orgcode = "skd",
                }
            };

            HttpClient client = SetupUtil.GetTestClient(_factory);
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
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            ServiceResource resource = new ServiceResource() { Identifier = "wrong_non_matcing_id" };

            HttpClient client = SetupUtil.GetTestClient(_factory);
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
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource() 
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string>() { { "en", "Super Duper Tjenestene" } },
                Description = new Dictionary<string, string>() { { "nb", "Dette er en super duper tjeneste" } },
                RightDescription = new Dictionary<string, string> { { "nb", "Dette gir mottakere rett til super duper tjeenste" } },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_Forbidden_NotResourceOwner()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string>() { { "en", "Super Duper Tjenestene" } },
                Description = new Dictionary<string, string>() { { "nb", "Dette er en super duper tjeneste" } },
                RightDescription = new Dictionary<string, string> { { "nb", "Dette gir mottakere rett til super duper tjeenste" } },
                Status = "Completed",
                ContactPoints = new List<ContactPoint>() { new ContactPoint() { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" } },
                HasCompetentAuthority = new Altinn.ResourceRegistry.Core.Models.CompetentAuthority()
                {
                    Organization = "991825827",
                    Orgcode = "digdir",
                }
            };

            string requestUri = "resourceregistry/api/v1/Resource/";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_AdminScope_OK_NotResourceOwner()
        {
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ServiceResource resource = new ServiceResource()
            {
                Identifier = "superdupertjenestene",
                Title = new Dictionary<string, string>() { { "en", "Super Duper Tjenestene" } },
                Description = new Dictionary<string, string>() { { "nb", "Dette er en super duper tjeneste"} },
                RightDescription = new Dictionary<string, string> { { "nb", "Dette gir mottakere rett til super duper tjeenste"} },
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateResource_UnAuthorized()
        {
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_OK_AdminScope_NotResourceOwner()
        {
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_Forbidden_NotResourceOwner()
        {
            string token = PrincipalUtil.GetOrgToken("ttd", "991825888", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_UnAuthorized()
        {
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

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Deletes a resource that user is authorized for
        /// Expected result: Return httpStatus No content statuscode
        /// </summary>
        [Fact]
        public async Task Delete_AuthorizedUser_ValidResource_ReturnsNoContent()
        {
            // Arrange            
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);            

            // Act
            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Deletes a resource that user is authorized for
        /// Expected result: Return httpStatus nocontent statuscode
        /// </summary>
        [Fact]
        public async Task Delete_AdminScope_ValidResource_ReturnsForbidden()
        {
            // Arrange            
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            // Act
            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

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
            // Arrange
            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            // Act
            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

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
            // Arrange            
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.write");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string resourceId = "altinn_access_management_skd";
            string requestUri = $"resourceregistry/api/v1/Resource/{resourceId}";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            // Act
            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Export_OK()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
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

    }
}