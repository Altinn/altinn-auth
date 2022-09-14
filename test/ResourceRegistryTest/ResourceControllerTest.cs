using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Mvc;
using ResourceRegistry.Controllers;
using ResourceRegistryTest.Utils;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
            string requestUri = "ResourceRegistry/api/Resource/altinn_access_management";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

            Assert.NotNull(resource);
            Assert.Equal("altinn_access_management", resource.Identifier);

        }


        [Fact]
        public async Task Test_Nav_Get()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "ResourceRegistry/api/Resource/nav_tiltakAvtaleOmArbeidstrening";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ServiceResource;

            Assert.NotNull(resource);
            Assert.NotNull(resource.Identifier);
            Assert.Equal("nav_tiltakAvtaleOmArbeidstrening", resource.Identifier);

        }

        [Fact]
        public async Task Search_Get()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "ResourceRegistry/api/Resource/Search?SearchTerm=test";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            List<ServiceResource>? resource = System.Text.Json.JsonSerializer.Deserialize<List<ServiceResource>>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as List<ServiceResource>;

            Assert.NotNull(resource);
            Assert.Equal(2, resource.Count);

        }


        [Fact]
        public async Task CreateResource_WithErrors()
        {

            ServiceResource resource = new ServiceResource() { Identifier = "superdupertjenestene" };
            resource.IsComplete = true;

            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "ResourceRegistry/api/Resource/";


            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            ValidationProblemDetails? errordetails = System.Text.Json.JsonSerializer.Deserialize<ValidationProblemDetails>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as ValidationProblemDetails;

            Assert.NotNull(errordetails);

            Assert.Equal(9, errordetails.Errors.Count);
        }


        [Fact]
        public async Task SetResourcePolicy_OK()
        {

            ServiceResource resource = new ServiceResource() { Identifier = "superdupertjenestene" };
            resource.IsComplete = false;

            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "ResourceRegistry/api/Resource/nav/policy";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }



        [Fact]
        public async Task UpdateResourcePolicy_OK()
        {
            ServiceResource resource = new ServiceResource() { Identifier = "superdupertjenestene" };
            resource.IsComplete = false;

            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "ResourceRegistry/api/Resource/nav/policy";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }


        [Fact]
        public async Task CreateResource_Ok()
        {

            ServiceResource resource = new ServiceResource() { Identifier = "superdupertjenestene" };
            resource.IsComplete = false;

            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "ResourceRegistry/api/Resource/";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json")
            };

            httpRequestMessage.Headers.Add("Accept", "application/json");
            httpRequestMessage.Headers.Add("ContentType", "application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);


            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}