using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Tests.Utils;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests
{
    public class Altinn2ExportControllerTest : IClassFixture<CustomWebApplicationFactory<ResourceController>>
    {

        private readonly CustomWebApplicationFactory<ResourceController> _factory;

        public Altinn2ExportControllerTest(CustomWebApplicationFactory<ResourceController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Export_Resource()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/altinn2export/resource?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }


        [Fact]
        public async Task Export_Policy()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/altinn2export/policy?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }
    }
}