using System.Net;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Microsoft.Extensions.Configuration;

// Migrated from WebApplicationFixture to ApiFixture as part of Phase 2.2
// Sub-step 16.3 (Step 16 — AccessMgmt.Tests WAF consolidation). Health
// endpoints have no DI dependencies of their own, so no extra services
// need to be registered — but we preserve appsettings.test.json to keep
// the existing configuration (Azure Storage, Cosmos, feature flags)
// matching what the legacy WebApplicationFixture provided.
namespace Altinn.AccessManagement.Tests.Health
{
    /// <summary>
    /// Health check
    /// </summary>
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
        public async Task VerifyHealthCheck_OK()
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
        public async Task VerifyAliveCheck_OK()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/alive");

            HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
