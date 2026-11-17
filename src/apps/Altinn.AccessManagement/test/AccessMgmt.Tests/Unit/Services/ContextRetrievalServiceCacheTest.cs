using System.Collections.Concurrent;
using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Models.SblBridge;
using Altinn.AccessManagement.Core.Services;
using Altinn.AccessManagement.Tests.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Platform.Register.Models;
using Authorization.Platform.Authorization.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.AccessManagement.Tests.Unit.Services;

/// <summary>
/// Cache behaviour of <see cref="ContextRetrievalService"/> on <see cref="HybridCache"/>: what is cached, what is not,
/// what reaches the distributed tier, and that every cached type survives the cache serializer.
/// </summary>
[UnitTest]
public class ContextRetrievalServiceCacheTest
{
    private readonly Mock<IResourceRegistryClient> _resourceRegistryClient = new(MockBehavior.Strict);
    private readonly Mock<IPartiesClient> _partiesClient = new(MockBehavior.Strict);
    private readonly RecordingDistributedCache _distributedCache = new();
    private readonly HybridCache _cache;
    private readonly ContextRetrievalService _sut;

    public ContextRetrievalServiceCacheTest()
    {
        _cache = HybridCacheUtil.Create(_distributedCache);
        _sut = new ContextRetrievalService(
            Options.Create(new CacheConfig
            {
                PolicyCacheTimeout = 5,
                AltinnRoleCacheTimeout = 5,
                PartyCacheTimeout = 5,
                MainUnitCacheTimeout = 5,
                KeyRolePartyIdsCacheTimeout = 5,
                ResourceRegistryResourceCacheTimeout = 5,
                ResourceRegistrySubjectResourcesCacheTimeout = 5,
            }),
            _cache,
            _resourceRegistryClient.Object,
            Mock.Of<IAltinnRolesClient>(),
            _partiesClient.Object,
            Mock.Of<IAuthenticationClient>(),
            Mock.Of<IProfileClient>());
    }

    [Fact]
    public async Task GetResource_SameIdTwice_CallsClientOnceAndWritesTheDistributedTier()
    {
        _resourceRegistryClient
            .Setup(c => c.GetResource("r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResource { Identifier = "r1", Title = new Dictionary<string, string> { ["nb"] = "Ressurs" } });

        ServiceResource first = await _sut.GetResource("r1", TestContext.Current.CancellationToken);
        ServiceResource second = await _sut.GetResource("r1", TestContext.Current.CancellationToken);

        second.Should().NotBeSameAs(first, "a hit returns a copy, not the cached instance");
        second.Title["nb"].Should().Be("Ressurs");
        _resourceRegistryClient.Verify(c => c.GetResource("r1", It.IsAny<CancellationToken>()), Times.Once);
        _distributedCache.WrittenKeys.Should().ContainSingle(k => k.Contains("resource:id:r1"));
    }

    [Fact]
    public async Task GetPartyForOrganization_ClientReturnsNull_IsNotCached()
    {
        _partiesClient
            .Setup(c => c.LookupPartyBySSNOrOrgNo(It.Is<PartyLookup>(l => l.OrgNo == "910000000"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Party)null);

        Party first = await _sut.GetPartyForOrganization("910000000", TestContext.Current.CancellationToken);
        Party second = await _sut.GetPartyForOrganization("910000000", TestContext.Current.CancellationToken);

        first.Should().BeNull();
        second.Should().BeNull();
        _partiesClient.Verify(c => c.LookupPartyBySSNOrOrgNo(It.IsAny<PartyLookup>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _distributedCache.WrittenKeys.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResourceList_Twice_ReturnsTheSameInstanceAndNeverReachesTheDistributedTier()
    {
        List<ServiceResource> list = [new ServiceResource { Identifier = "r1" }, new ServiceResource { Identifier = "r2" }];
        _resourceRegistryClient
            .Setup(c => c.GetResourceList(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        List<ServiceResource> first = await _sut.GetResourceList(TestContext.Current.CancellationToken);
        List<ServiceResource> second = await _sut.GetResourceList(TestContext.Current.CancellationToken);

        first.Should().BeSameAs(list);
        second.Should().BeSameAs(list, "the whole-registry list is kept in memory as one instance, never serialized");
        _resourceRegistryClient.Verify(c => c.GetResourceList(It.IsAny<CancellationToken>()), Times.Once);
        _distributedCache.WrittenKeys.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryCachedType_RoundTripsThroughTheCacheSerializer()
    {
        await AssertRoundTrip(new Party
        {
            PartyId = 50001,
            PartyUuid = Guid.Parse("00000000-0000-0000-0000-000000050001"),
            Name = "Testorganisasjon",
            OrgNumber = "910000000",
            ChildParties = [new Party { PartyId = 50002, Name = "Underenhet" }],
        });
        await AssertRoundTrip(new ServiceResource
        {
            Identifier = "app_ttd_test",
            Title = new Dictionary<string, string> { ["nb"] = "Test", ["en"] = "Test" },
            ResourceType = ResourceType.AltinnApp,
            ResourceReferences = [new ResourceReference { ReferenceType = ReferenceType.ApplicationId, Reference = "ttd/test" }],
            AuthorizationReference = [new AttributeMatch { Id = "urn:altinn:org", Value = "ttd" }],
        });
        await AssertRoundTrip(new List<RightDto>
        {
            new() { Key = "read", Name = "Les", Action = new AttributeDto { Type = "urn:oasis:names:tc:xacml:1.0:action:action-id", Value = "read" }, Resource = [new AttributeDto { Type = "urn:altinn:resource", Value = "r1" }] },
        });
        await AssertRoundTrip(new List<Role> { new() { Type = "altinn", Value = "DAGL" } });
        await AssertRoundTrip(new MainUnit { PartyId = 50001, SubunitPartyId = 50002, OrganizationNumber = "910000000", OrganizationName = "Hovedenhet" });
        await AssertRoundTrip(new NewUserProfile { UserId = 20001, UserUuid = Guid.Parse("00000000-0000-0000-0000-000000020001"), UserName = "test", Email = "test@example.com" });
        await AssertRoundTrip<IEnumerable<BaseAttribute>>([new BaseAttribute { Type = "urn:altinn:resource", Value = "r1" }]);
        await AssertRoundTrip(new List<int> { 50001, 50002 });
    }

    private async Task AssertRoundTrip<T>(T value)
    {
        string key = $"roundtrip:{typeof(T).FullName}";
        await _cache.SetAsync(key, value, cancellationToken: TestContext.Current.CancellationToken);
        T read = await _cache.GetOrCreateAsync<T>(key, _ => throw new InvalidOperationException("expected a cache hit"), cancellationToken: TestContext.Current.CancellationToken);

        JsonSerializer.Serialize(read).Should().Be(JsonSerializer.Serialize(value), $"{typeof(T).Name} must survive the cache serializer");
    }

    /// <summary>
    /// A distributed tier that remembers which keys were written, so a test can tell what leaves the process.
    /// </summary>
    private sealed class RecordingDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _entries = new();

        public List<string> WrittenKeys { get; } = [];

        public byte[] Get(string key) => _entries.TryGetValue(key, out byte[] value) ? value : null;

        public Task<byte[]> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            WrittenKeys.Add(key);
            _entries[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _entries.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
