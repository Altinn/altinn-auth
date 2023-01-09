using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Mvc;
using ResourceRegistry.Controllers;
using ResourceRegistryTest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ResourceRegistryTest
{
    public class ResourceControllerTest : IClassFixture<CustomWebApplicationFactory<ResourceController>>
    {
        private readonly CustomWebApplicationFactory<ResourceController> _factory;

        public ResourceControllerTest(CustomWebApplicationFactory<ResourceController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetResource_altinn_access_management_OK()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
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
        public async Task Test_Nav_Get()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
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
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/Resource/Search?SearchTerm=test";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(2, resource.Count);
        }

        [Fact]
        public async Task CreateResource_WithErrors()
        {
            ServiceResource resource = new ServiceResource() { Identifier = "superdupertjenestene" };
            resource.IsComplete = true;

            HttpClient client = SetupUtil.GetTestClient(_factory);
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

            Assert.Equal(9, errordetails.Errors.Count);
        }

        [Fact]
        public async Task SetResourcePolicy_OK()
        {
            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            string fileName = $"{resource.Identifier}.xml";
            string filePath = $"Data/ResourcePolicies/{fileName}";

            Uri requestUri = new Uri($"resourceregistry/api/v1/Resource/{resource.Identifier}/policy", UriKind.Relative);

            ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

            MultipartFormDataContent content = new();
            content.Add(fileContent, "policyFile", fileName);

            HttpRequestMessage httpRequestMessage = new() { Method = HttpMethod.Post, RequestUri = requestUri, Content = content };
            httpRequestMessage.Headers.Add("ContentType", "multipart/form-data");

            HttpClient client = SetupUtil.GetTestClient(_factory);
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task SetResourcePolicy_Invalid_UnknownResource()
        {
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

            HttpClient client = SetupUtil.GetTestClient(_factory);
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Unknown resource", responseContent);
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

            HttpClient client = SetupUtil.GetTestClient(_factory);
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateResourcePolicy_InvalidResourceId()
        {
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

            HttpClient client = SetupUtil.GetTestClient(_factory);
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Policy not accepted: Contains rules for a different registry resource", responseContent);
        }

        [Fact]
        public async Task UpdateResourcePolicy_MissingResourceId()
        {
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

            HttpClient client = SetupUtil.GetTestClient(_factory);
            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Policy not accepted: Contains rule without reference to registry resource id", responseContent);
        }

        [Fact]
        public async Task UpdateResource_Ok()
        {
            ServiceResource resource = new ServiceResource() { Identifier = "altinn_access_management" };
            resource.IsComplete = false;

            HttpClient client = SetupUtil.GetTestClient(_factory);
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
            ServiceResource resource = new ServiceResource() { Identifier = "wrong_non_matcing_id" };
            resource.IsComplete = false;

            HttpClient client = SetupUtil.GetTestClient(_factory);
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
            ServiceResource resource = new ServiceResource() { Identifier = "superdupertjenestene" };
            resource.IsComplete = false;

            HttpClient client = SetupUtil.GetTestClient(_factory);
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
    }
}