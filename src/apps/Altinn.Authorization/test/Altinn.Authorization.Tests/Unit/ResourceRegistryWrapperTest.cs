using System.Net;
using System.Text;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.Api.Contracts.ResourceRegistry;
using Altinn.Authorization.Api.Contracts.ResourceRegistry.Enums;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Authorization.Services.Implementation;
using Altinn.Authorization.Tests.Util;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.ResourceRegistry.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.Authorization.Tests.Unit
{
    [UnitTest]
    public class ResourceRegistryWrapperTest
    {
        private const string RESOURCE = "ttd-accesslist-resource";

        private readonly QueuedHandler _handler = new QueuedHandler();
        private readonly ResourceRegistryWrapper _wrapper;

        public ResourceRegistryWrapperTest()
        {
            _wrapper = CreateWrapper(TestSetupUtil.CreateHybridCache());
        }

        /// <summary>
        /// Test case: Get the policy of the same resource twice.
        /// Expected: The registry is called once and both reads return the same parsed policy instance, since the
        /// parsed policy is held in the local cache rather than copied on every read.
        /// </summary>
        [Fact]
        public async Task GetResourcePolicyAsync_SameResourceRequestedTwice_ReturnsSameInstanceAndCallsRegistryOnce()
        {
            // Arrange
            _handler.Enqueue(HttpStatusCode.OK, GetPolicyXml("first"));
            _handler.Enqueue(HttpStatusCode.OK, GetPolicyXml("second"));

            // Act
            XacmlPolicy first = await _wrapper.GetResourcePolicyAsync(RESOURCE, TestContext.Current.CancellationToken);
            XacmlPolicy second = await _wrapper.GetResourcePolicyAsync(RESOURCE, TestContext.Current.CancellationToken);

            // Assert
            first.PolicyId.ToString().Should().Be("urn:altinn:policyid:first");
            second.Should().BeSameAs(first);
            _handler.Requests.Should().HaveCount(1);
        }

        /// <summary>
        /// Test case: The registry answers 404 for the policy, then 200.
        /// Expected: The first read returns null without caching it, so the second read reaches the registry and returns the policy.
        /// </summary>
        [Fact]
        public async Task GetResourcePolicyAsync_RegistryReturnsNotFound_ReturnsNullAndDoesNotCacheIt()
        {
            // Arrange
            _handler.Enqueue(HttpStatusCode.NotFound, string.Empty);
            _handler.Enqueue(HttpStatusCode.OK, GetPolicyXml("later"));

            // Act
            XacmlPolicy first = await _wrapper.GetResourcePolicyAsync(RESOURCE, TestContext.Current.CancellationToken);
            XacmlPolicy second = await _wrapper.GetResourcePolicyAsync(RESOURCE, TestContext.Current.CancellationToken);

            // Assert
            first.Should().BeNull();
            second.PolicyId.ToString().Should().Be("urn:altinn:policyid:later");
            _handler.Requests.Should().HaveCount(2);
        }

        /// <summary>
        /// Test case: The registry answers 200 with an empty body, then with a policy.
        /// Expected: The empty policy is returned as null and not cached, so the second read returns the policy.
        /// </summary>
        [Fact]
        public async Task GetResourcePolicyAsync_RegistryReturnsEmptyPolicy_ReturnsNullAndDoesNotCacheIt()
        {
            // Arrange
            _handler.Enqueue(HttpStatusCode.OK, string.Empty);
            _handler.Enqueue(HttpStatusCode.OK, GetPolicyXml("later"));

            // Act
            XacmlPolicy first = await _wrapper.GetResourcePolicyAsync(RESOURCE, TestContext.Current.CancellationToken);
            XacmlPolicy second = await _wrapper.GetResourcePolicyAsync(RESOURCE, TestContext.Current.CancellationToken);

            // Assert
            first.Should().BeNull();
            second.PolicyId.ToString().Should().Be("urn:altinn:policyid:later");
            _handler.Requests.Should().HaveCount(2);
        }

        /// <summary>
        /// Test case: Get the same resource twice.
        /// Expected: The registry is called once and the second read returns the cached instance, not a deserialized copy.
        /// </summary>
        [Fact]
        public async Task GetResourceAsync_SameResourceRequestedTwice_ReturnsResourceAndCallsRegistryOnce()
        {
            // Arrange
            _handler.Enqueue(HttpStatusCode.OK, """{"identifier":"ttd-accesslist-resource","title":{"nb":"Tilgangsliste"},"resourceType":"GenericAccessResource","accessListMode":"Enabled"}""");

            // Act
            ServiceResource first = await _wrapper.GetResourceAsync(RESOURCE, TestContext.Current.CancellationToken);
            ServiceResource second = await _wrapper.GetResourceAsync(RESOURCE, TestContext.Current.CancellationToken);

            // Assert
            first.Identifier.Should().Be(RESOURCE);
            second.Should().BeSameAs(first);
            _handler.Requests.Should().HaveCount(1);
        }

        /// <summary>
        /// Test case: One instance caches a resource, and a second instance that shares the distributed tier reads it.
        /// Expected: The second read comes from the distributed tier, so the registry is called once.
        /// </summary>
        [Fact]
        public async Task GetResourceAsync_CachedByAnotherInstance_ReadsDistributedTier()
        {
            // Arrange
            IDistributedCache shared = new SharedDistributedCache();
            ResourceRegistryWrapper other = CreateWrapper(CreateHybridCache(shared));
            ResourceRegistryWrapper wrapper = CreateWrapper(CreateHybridCache(shared));
            _handler.Enqueue(HttpStatusCode.OK, """{"identifier":"ttd-accesslist-resource","title":{"nb":"Tilgangsliste"},"resourceType":"GenericAccessResource","accessListMode":"Enabled"}""");

            // Act
            ServiceResource first = await other.GetResourceAsync(RESOURCE, TestContext.Current.CancellationToken);
            ServiceResource second = await wrapper.GetResourceAsync(RESOURCE, TestContext.Current.CancellationToken);

            // Assert
            second.Should().BeEquivalentTo(first);
            second.AccessListMode.Should().Be(ResourceAccessListMode.Enabled);
            _handler.Requests.Should().HaveCount(1);
        }

        /// <summary>
        /// Test case: Get the access list memberships of a party for a resource twice.
        /// Expected: The registry is called once and the second read returns the memberships with their action filters.
        /// </summary>
        [Fact]
        public async Task GetMembershipsForResourceForParty_SameLookupTwice_ReturnsMembershipsAndCallsRegistryOnce()
        {
            // Arrange
            PartyUrn party = PartyUrn.PartyUuid.Create(Guid.Parse("00000000-0000-0000-0005-000000005545"));
            ResourceIdUrn resource = ResourceIdUrn.ResourceId.Create(ResourceIdentifier.Parse(RESOURCE));
            _handler.Enqueue(HttpStatusCode.OK, """{"data":[{"party":"urn:altinn:party:uuid:00000000-0000-0000-0005-000000005545","resource":"urn:altinn:resource:ttd-accesslist-resource","since":"2024-08-27T15:15:55.446051+00:00","actionFilters":["read"]}]}""");

            // Act
            IEnumerable<AccessListResourceMembershipWithActionFilterDto> first = await _wrapper.GetMembershipsForResourceForParty(party, resource, TestContext.Current.CancellationToken);
            IEnumerable<AccessListResourceMembershipWithActionFilterDto> second = await _wrapper.GetMembershipsForResourceForParty(party, resource, TestContext.Current.CancellationToken);

            // Assert
            AccessListResourceMembershipWithActionFilterDto membership = first.Should().ContainSingle().Subject;
            membership.Resource.ToString().Should().Be("urn:altinn:resource:ttd-accesslist-resource");
            membership.ActionFilters.Should().Equal("read");
            AccessListResourceMembershipWithActionFilterDto cached = second.Should().ContainSingle().Subject;
            cached.Party.Should().Be(membership.Party);
            cached.ActionFilters.Should().Equal(membership.ActionFilters);
            _handler.Requests.Should().HaveCount(1);
        }

        /// <summary>
        /// Test case: Get the access lists a party is a member of twice.
        /// Expected: The registry is called once and the second read returns the same access lists.
        /// </summary>
        [Fact]
        public async Task GetMembershipsForParty_SamePartyTwice_ReturnsAccessListsAndCallsRegistryOnce()
        {
            // Arrange
            PartyUrn.PartyUuid party = PartyUrn.PartyUuid.Create(Guid.Parse("00000000-0000-0000-0005-000000005545"));
            _handler.Enqueue(HttpStatusCode.OK, """[{"urn":"urn:altinn:access-list:ttd:tilgangsliste","identifier":"tilgangsliste","name":"Tilgangsliste","description":"Testliste"}]""");

            // Act
            IEnumerable<AccessListInfoDto> first = await _wrapper.GetMembershipsForParty(party, TestContext.Current.CancellationToken);
            IEnumerable<AccessListInfoDto> second = await _wrapper.GetMembershipsForParty(party, TestContext.Current.CancellationToken);

            // Assert
            first.Should().ContainSingle().Which.Identifier.Should().Be("tilgangsliste");
            second.Should().Equal(first);
            _handler.Requests.Should().HaveCount(1);
        }

        private static string GetPolicyXml(string id) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <xacml:Policy xmlns:xacml="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17" PolicyId="urn:altinn:policyid:{id}" Version="1.0" RuleCombiningAlgId="urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides">
              <xacml:Target />
              <xacml:Rule RuleId="urn:altinn:ruleid:{id}" Effect="Permit">
                <xacml:Target />
              </xacml:Rule>
            </xacml:Policy>
            """;

        private static HybridCache CreateHybridCache(IDistributedCache distributedCache)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(distributedCache);
            services.AddHybridCache();
            return services.BuildServiceProvider().GetRequiredService<HybridCache>();
        }

        private ResourceRegistryWrapper CreateWrapper(HybridCache cache)
        {
            ResourceRegistryClient registryClient = new ResourceRegistryClient(
                new HttpClient(_handler),
                Options.Create(new PlatformSettings { ApiResourceRegistryEndpoint = "https://platform.example.com/resourceregistry/api/v1/" }));

            return new ResourceRegistryWrapper(
                registryClient,
                Mock.Of<IAccessTokenGenerator>(g => g.GenerateAccessToken("platform", "authorization") == "token"),
                cache,
                Options.Create(new GeneralSettings { PolicyCacheTimeout = 1 }));
        }

        /// <summary>
        /// A distributed cache two HybridCache instances can share. HybridCache ignores a MemoryDistributedCache as its
        /// distributed tier, so the test wraps one.
        /// </summary>
        private sealed class SharedDistributedCache : IDistributedCache
        {
            private readonly MemoryDistributedCache _inner = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

            public byte[] Get(string key) => _inner.Get(key);

            public Task<byte[]> GetAsync(string key, CancellationToken token = default) => _inner.GetAsync(key, token);

            public void Refresh(string key) => _inner.Refresh(key);

            public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);

            public void Remove(string key) => _inner.Remove(key);

            public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _inner.Set(key, value, options);

            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => _inner.SetAsync(key, value, options, token);
        }

        /// <summary>
        /// Answers each request with the next queued response and records the requests it saw.
        /// </summary>
        private sealed class QueuedHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new Queue<HttpResponseMessage>();

            public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

            public void Enqueue(HttpStatusCode status, string body)
            {
                _responses.Enqueue(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(_responses.Dequeue());
            }
        }
    }
}
