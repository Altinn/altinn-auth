using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.TestUtils;

namespace Altinn.ResourceRegistry.Tests
{
    public class ResourceOwnerControllerTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Orglist_OK()
        {
            HttpClient client = CreateClient();
            string requestUri = "resourceregistry/api/v1/resource/orgs";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            string responseContent = await response.Content.ReadAsStringAsync();

            OrgList? orgList = System.Text.Json.JsonSerializer.Deserialize<OrgList>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            
            Assert.NotNull(orgList);
            Assert.True(orgList.Orgs.Keys.Count > 10);
        }
    }
}
