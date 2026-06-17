using System.Net;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Microsoft.Extensions.Configuration;

// Health endpoints have no DI dependencies of their own, so no extra services
// need to be registered — appsettings.test.json is preserved to keep the
// configuration (Azure Storage, Cosmos, feature flags) the app expects.
namespace Altinn.AccessManagement.Tests.Integration.Health
{
    /// <summary>
    /// Health check
    /// </summary>
    [IntegrationTest]
    public class HealthCheckTests : IClassFixture<ApiFixture>
    {
        private readonly HttpClient _client;

        public HealthCheckTests(ApiFixture fixture)
        {
            fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
            _client = fixture.CreateClient();
        }

        /// <summary>
        /// Verify that component responds on health check
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HealthEndpoint_WhenAppIsHealthy_Returns200WithHealthyStatus()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");

            HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Verify that component responds on alive check
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AliveEndpoint_WhenAppIsRunning_Returns200Ok()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/alive");

            HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
