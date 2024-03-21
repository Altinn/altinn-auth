using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests
{
    public class ResourceOwnerControllerTest : IClassFixture<CustomWebApplicationFactory<ResourceOwnerController>>
    {

        private readonly CustomWebApplicationFactory<ResourceOwnerController> _factory;

        public ResourceOwnerControllerTest(CustomWebApplicationFactory<ResourceOwnerController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Orglist_OK()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
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
