using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Tests.Utils;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests
{
    public class ExportControllerTest : IClassFixture<CustomWebApplicationFactory<ResourceController>>
    {

        private readonly CustomWebApplicationFactory<ResourceController> _factory;

        public ExportControllerTest(CustomWebApplicationFactory<ResourceController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Export_OK()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/resource/export";

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