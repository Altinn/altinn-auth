using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Altinn.Authorization.Tests.Fixtures;
using Altinn.Authorization.Tests.MockServices;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Altinn.Authorization.Tests.Integration
{
    /// <summary>
    /// Regression tests for #4742: the delegation list the PDP caches per subject, party and resource is
    /// filtered by instance id after the cache lookup, so an instance delegated after the list was cached
    /// stayed invisible until the entry expired. A lookup for an instance now accepts a cached list only
    /// for ten seconds after it was fetched; lookups without an instance keep the five-minute cache.
    /// </summary>
    [IntegrationTest]
    public class DelegationCacheExpiryTests : IClassFixture<AuthorizationApiFixture>
    {
        private const string TestCase = "InstanceDelegationPolicy_AltinnAppsOrg1App1_Permit";
        private const string InstanceA = "f8d3526c-596b-4322-a041-38a8925c2a82";
        private const string InstanceB = "0d5c4a8e-3b2f-4e6a-9c1d-7f8e9a0b1c2d";

        private readonly HttpClient _client;
        private readonly FakeTimeProvider _clock;
        private readonly AccessManagementWrapperMock _accessManagement;

        public DelegationCacheExpiryTests(AuthorizationApiFixture fixture)
        {
            fixture.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FakeTimeProvider(DateTimeOffset.UtcNow)));

            _client = fixture.BuildClient();
            _clock = (FakeTimeProvider)fixture.Services.GetRequiredService<TimeProvider>();
            _accessManagement = (AccessManagementWrapperMock)fixture.Services.GetRequiredService<IAccessManagementWrapper>();

            // Both tests use the same cache key, so each starts from an empty cache.
            ((MemoryCache)fixture.Services.GetRequiredService<IMemoryCache>()).Clear();
        }

        [Fact]
        public async Task PDP_Decision_InstanceScopedDelegationLookup_ExpiresAfterTenSeconds()
        {
            int before = _accessManagement.DelegationChangeLookups;

            await Authorize(InstanceA);
            Assert.Equal(before + 1, _accessManagement.DelegationChangeLookups);

            // The same instance within the window is served from the cache.
            await Authorize(InstanceA);
            Assert.Equal(before + 1, _accessManagement.DelegationChangeLookups);

            // Another instance of the same resource within the window reuses the list, the key does not hold the instance.
            await Authorize(InstanceB);
            Assert.Equal(before + 1, _accessManagement.DelegationChangeLookups);

            // Once the short window has passed, the other instance gets a fresh lookup.
            _clock.Advance(TimeSpan.FromSeconds(11));
            await Authorize(InstanceB);
            Assert.Equal(before + 2, _accessManagement.DelegationChangeLookups);
        }

        [Fact]
        public async Task PDP_Decision_ListCachedWithoutInstance_IsNotReusedForAnInstanceAfterTenSeconds()
        {
            int before = _accessManagement.DelegationChangeLookups;

            await Authorize(instanceId: null);
            Assert.Equal(before + 1, _accessManagement.DelegationChangeLookups);

            // A lookup without an instance keeps the five-minute cache.
            _clock.Advance(TimeSpan.FromSeconds(11));
            await Authorize(instanceId: null);
            Assert.Equal(before + 1, _accessManagement.DelegationChangeLookups);

            // A lookup for an instance does not accept a list older than ten seconds, whichever request cached it.
            await Authorize(InstanceA);
            Assert.Equal(before + 2, _accessManagement.DelegationChangeLookups);
        }

        /// <summary>
        /// Posts the test case's request with <c>urn:altinn:resource:instance-id</c> set to <paramref name="instanceId"/>,
        /// or without that attribute when it is null. Subject, party and resource stay the same, so every variant shares one cache key.
        /// </summary>
        private async Task Authorize(string instanceId)
        {
            JsonNode request = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Xacml", "3.0", "AltinnApps", TestCase + "Request.json")));
            JsonArray attributes = request["Request"]["Resource"][0]["Attribute"].AsArray();
            JsonNode instance = attributes.Single(a => a["AttributeId"].GetValue<string>() == "urn:altinn:resource:instance-id");
            if (instanceId == null)
            {
                attributes.Remove(instance);
            }
            else
            {
                instance["Value"] = instanceId;
            }

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "authorization/api/v1/Decision")
            {
                Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json")
            };
            message.Headers.Add("testcase", TestCase);
            message.Headers.Add("Accept", "application/json");

            HttpResponseMessage response = await _client.SendAsync(message);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
