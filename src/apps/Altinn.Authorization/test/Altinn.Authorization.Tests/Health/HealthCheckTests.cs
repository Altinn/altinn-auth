using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.IntegrationTests.Fixtures;
using Xunit;

namespace Altinn.Platform.Authorization.IntegrationTests.Health
{
    /// <summary>
    /// Health check 
    /// </summary>
    public class HealthCheckTests : IClassFixture<AuthorizationApiFixture>
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="fixture">The shared authorization API fixture</param>
        public HealthCheckTests(AuthorizationApiFixture fixture)
        {
            _client = fixture.BuildClient();
        }

        /// <summary>
        /// Verify that component responds on health check
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyHealthCheck_OK()
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/health");

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
