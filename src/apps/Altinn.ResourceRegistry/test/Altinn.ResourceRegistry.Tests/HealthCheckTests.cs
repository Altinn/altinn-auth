using System.Net;
using Altinn.ResourceRegistry.TestUtils;

namespace Altinn.ResourceRegistry.Tests
{
    public class HealthCheckTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        /// <summary>
        /// Verify that component responds on health check
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyHeltCheck_OK()
        {
            HttpClient client = CreateClient();

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/health")
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify that component responds on health check
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyAliveCheck_OK()
        {
            HttpClient client = CreateClient();

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/alive")
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
