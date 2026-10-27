using System.Net;
using System.Text;
using Altinn.Authorization.Tests.Fixtures;
using Altinn.Authorization.Tests.MockServices;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Altinn.Authorization.Tests.Integration
{
    /// <summary>
    /// Regression tests for #4742: the delegation list the PDP caches per subject, party and resource is
    /// filtered by instance id after the cache lookup, so an instance delegated after the list was cached
    /// stayed invisible until the entry expired. Instance-scoped lookups now expire after ten seconds.
    /// </summary>
    [IntegrationTest]
    public class DelegationCacheExpiryTests : IClassFixture<AuthorizationApiFixture>
    {
        private const string TestCase = "InstanceDelegationPolicy_AltinnAppsOrg1App1_Permit";
        private const string InstanceA = "f8d3526c-596b-4322-a041-38a8925c2a82";
        private const string InstanceB = "0d5c4a8e-3b2f-4e6a-9c1d-7f8e9a0b1c2d";

        private readonly HttpClient _client;
        private readonly FakeClock _clock;
        private readonly AccessManagementWrapperMock _accessManagement;

        public DelegationCacheExpiryTests(AuthorizationApiFixture fixture)
        {
            fixture.ConfigureServices(services =>
            {
                services.AddSingleton<ISystemClock, FakeClock>();
                services.AddSingleton<IMemoryCache>(sp => new MemoryCache(new MemoryCacheOptions { Clock = sp.GetRequiredService<ISystemClock>() }));
            });

            _client = fixture.BuildClient();
            _clock = (FakeClock)fixture.Services.GetRequiredService<ISystemClock>();
            _accessManagement = (AccessManagementWrapperMock)fixture.Services.GetRequiredService<IAccessManagementWrapper>();
        }

        [Fact]
        public async Task PDP_Decision_InstanceScopedDelegationLookup_ExpiresAfterTenSeconds()
        {
            await Authorize(InstanceA);
            Assert.Equal(1, _accessManagement.DelegationChangeLookups);

            // The same instance within the window is served from the cache.
            await Authorize(InstanceA);
            Assert.Equal(1, _accessManagement.DelegationChangeLookups);

            // Another instance of the same resource within the window reuses the list, the key does not hold the instance.
            await Authorize(InstanceB);
            Assert.Equal(1, _accessManagement.DelegationChangeLookups);

            // Once the short expiry has passed, the other instance gets a fresh lookup.
            _clock.UtcNow += TimeSpan.FromSeconds(11);
            await Authorize(InstanceB);
            Assert.Equal(2, _accessManagement.DelegationChangeLookups);
        }

        private async Task Authorize(string instanceId)
        {
            string requestText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Xacml", "3.0", "AltinnApps", TestCase + "Request.json")).Replace(InstanceA, instanceId);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "authorization/api/v1/Decision")
            {
                Content = new StringContent(requestText, Encoding.UTF8, "application/json")
            };
            message.Headers.Add("testcase", TestCase);
            message.Headers.Add("Accept", "application/json");

            HttpResponseMessage response = await _client.SendAsync(message);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private sealed class FakeClock : ISystemClock
        {
            public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        }
    }
}
