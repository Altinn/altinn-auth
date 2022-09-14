using Altinn.ResourceRegistry.Controllers;
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
    public class ExportControllerTest : IClassFixture<CustomWebApplicationFactory<ExportController>>
    {

        private readonly CustomWebApplicationFactory<ExportController> _factory;

        public ExportControllerTest(CustomWebApplicationFactory<ExportController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Export_OK()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "ResourceRegistry/api/export";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();
            // System.IO.File.WriteAllText("rdf.ttl", responseContent);
            Assert.NotNull(responseContent);
        }

    }
}