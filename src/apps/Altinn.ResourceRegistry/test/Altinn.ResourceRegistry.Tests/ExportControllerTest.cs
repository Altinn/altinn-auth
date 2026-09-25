using Altinn.ResourceRegistry.TestUtils;

namespace Altinn.ResourceRegistry.Tests
{
    public class ExportControllerTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Export_OK()
        {
            HttpClient client = CreateClient();
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
