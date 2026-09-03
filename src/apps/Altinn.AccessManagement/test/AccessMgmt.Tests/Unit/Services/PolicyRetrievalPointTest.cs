using System.Text;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services;
using Altinn.Authorization.ABAC.Xacml;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.AccessManagement.Tests.Unit.Services;

/// <summary>
/// Unit tests for the memory cache behaviour of <see cref="PolicyRetrievalPoint"/>.
/// </summary>
[UnitTest]
public class PolicyRetrievalPointTest
{
    private const string ResourceId = "altinn_access_management";
    private const string PolicyPath = "altinn_access_management/resourcepolicy.xml";

    private readonly Mock<IPolicyRepository> _policyRepository = new(MockBehavior.Strict);
    private readonly Mock<IPolicyFactory> _policyFactory = new(MockBehavior.Strict);
    private readonly PolicyRetrievalPoint _sut;

    public PolicyRetrievalPointTest()
    {
        _policyFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(_policyRepository.Object);

        _sut = new PolicyRetrievalPoint(
            _policyFactory.Object,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new CacheConfig { PolicyCacheTimeout = 1 }));
    }

    [Fact]
    public async Task GetPolicyAsync_SameResourceRequestedTwice_ReadsPolicyFromRepositoryOnce()
    {
        _policyRepository
            .Setup(r => r.GetPolicyAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(PolicyStream("current")));

        XacmlPolicy first = await _sut.GetPolicyAsync(ResourceId, TestContext.Current.CancellationToken);
        XacmlPolicy second = await _sut.GetPolicyAsync(ResourceId, TestContext.Current.CancellationToken);

        first.PolicyId.ToString().Should().Be("urn:altinn:policyid:current");
        second.PolicyId.ToString().Should().Be("urn:altinn:policyid:current");
        _policyRepository.Verify(r => r.GetPolicyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPolicyVersionAsync_SameVersionRequestedTwice_ReadsPolicyFromRepositoryOnce()
    {
        _policyRepository
            .Setup(r => r.GetPolicyVersionAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(PolicyStream("v1")));

        XacmlPolicy first = await _sut.GetPolicyVersionAsync(PolicyPath, "v1", TestContext.Current.CancellationToken);
        XacmlPolicy second = await _sut.GetPolicyVersionAsync(PolicyPath, "v1", TestContext.Current.CancellationToken);

        first.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
        second.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
        _policyRepository.Verify(r => r.GetPolicyVersionAsync("v1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPolicyVersionAsync_DifferentVersionsOfSamePolicy_ReturnsEachVersionAndCachesThemSeparately()
    {
        _policyRepository
            .Setup(r => r.GetPolicyVersionAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(PolicyStream("v1")));
        _policyRepository
            .Setup(r => r.GetPolicyVersionAsync("v2", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(PolicyStream("v2")));

        XacmlPolicy v1 = await _sut.GetPolicyVersionAsync(PolicyPath, "v1", TestContext.Current.CancellationToken);
        XacmlPolicy v2 = await _sut.GetPolicyVersionAsync(PolicyPath, "v2", TestContext.Current.CancellationToken);
        XacmlPolicy v1Again = await _sut.GetPolicyVersionAsync(PolicyPath, "v1", TestContext.Current.CancellationToken);

        v1.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
        v2.PolicyId.ToString().Should().Be("urn:altinn:policyid:v2");
        v1Again.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
        _policyRepository.Verify(r => r.GetPolicyVersionAsync("v1", It.IsAny<CancellationToken>()), Times.Once);
        _policyRepository.Verify(r => r.GetPolicyVersionAsync("v2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPolicyVersionAsync_CallerClearedRulesOnPreviousResult_ReturnsPolicyWithOriginalRules()
    {
        _policyRepository
            .Setup(r => r.GetPolicyVersionAsync("v1", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(PolicyStream("v1")));

        XacmlPolicy first = await _sut.GetPolicyVersionAsync(PolicyPath, "v1", TestContext.Current.CancellationToken);
        first.Rules.Should().ContainSingle();
        first.Rules.Clear();

        XacmlPolicy second = await _sut.GetPolicyVersionAsync(PolicyPath, "v1", TestContext.Current.CancellationToken);

        second.Should().NotBeSameAs(first);
        second.Rules.Should().ContainSingle().Which.RuleId.Should().Be("urn:altinn:ruleid:v1");
        _policyRepository.Verify(r => r.GetPolicyVersionAsync("v1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPolicyVersionAsync_VersionMissingThenRequestedAgain_ReturnsNullAndReadsRepositoryAgain()
    {
        _policyRepository
            .Setup(r => r.GetPolicyVersionAsync("v9", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream()));

        XacmlPolicy first = await _sut.GetPolicyVersionAsync(PolicyPath, "v9", TestContext.Current.CancellationToken);
        XacmlPolicy second = await _sut.GetPolicyVersionAsync(PolicyPath, "v9", TestContext.Current.CancellationToken);

        first.Should().BeNull();
        second.Should().BeNull();
        _policyRepository.Verify(r => r.GetPolicyVersionAsync("v9", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPolicyVersionAsync_DocumentFailsToParse_IsNotCachedAndIsReadAgain()
    {
        _policyRepository
            .Setup(r => r.GetPolicyVersionAsync("v8", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("<not-a-policy>"))));

        Func<Task> first = () => _sut.GetPolicyVersionAsync(PolicyPath, "v8", TestContext.Current.CancellationToken);
        Func<Task> second = () => _sut.GetPolicyVersionAsync(PolicyPath, "v8", TestContext.Current.CancellationToken);

        await first.Should().ThrowAsync<Exception>();
        await second.Should().ThrowAsync<Exception>();
        _policyRepository.Verify(r => r.GetPolicyVersionAsync("v8", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static Stream PolicyStream(string id) =>
        new MemoryStream(Encoding.UTF8.GetBytes($"""
            <?xml version="1.0" encoding="utf-8"?>
            <xacml:Policy xmlns:xacml="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17" PolicyId="urn:altinn:policyid:{id}" Version="1.0" RuleCombiningAlgId="urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides">
              <xacml:Target />
              <xacml:Rule RuleId="urn:altinn:ruleid:{id}" Effect="Permit">
                <xacml:Target />
              </xacml:Rule>
            </xacml:Policy>
            """));
}
