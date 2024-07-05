using Altinn.ResourceRegistry.TestUtils;

namespace Altinn.ResourceRegistry.Tests
{
    public class Altinn2ExportControllerTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Export_Resource()
        {
            HttpClient client = CreateClient();
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
            HttpClient client = CreateClient();
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
