using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Moq;

namespace Altinn.Platform.Authorization.Tests;

public class PolicyInformationPointTest
{
    private readonly Mock<IPolicyRetrievalPoint> _prpMock = new();
    private readonly Mock<IDelegationMetadataRepository> _delegationRepoMock = new();

    private PolicyInformationPoint CreateSut() => new(_prpMock.Object, _delegationRepoMock.Object);

    [Fact]
    public async Task GetRulesAsync_NoDelegationChanges_ReturnsEmptyList()
    {
        _delegationRepoMock
            .Setup(r => r.GetAllCurrentDelegationChanges(It.IsAny<List<int>>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new List<DelegationChange>());

        var sut = CreateSut();
        var result = await sut.GetRulesAsync(["org/app"], [1000], [2000], []);

        Assert.Empty(result);
        _prpMock.Verify(p => p.GetPolicyVersionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRulesAsync_RevokeLast_SkipsDelegation()
    {
        _delegationRepoMock
            .Setup(r => r.GetAllCurrentDelegationChanges(It.IsAny<List<int>>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(
            [
                new DelegationChange
                {
                    DelegationChangeType = DelegationChangeType.RevokeLast,
                    OfferedByPartyId = 1000,
                    BlobStoragePolicyPath = "path",
                    BlobStorageVersionId = "v1"
                }
            ]);

        var sut = CreateSut();
        var result = await sut.GetRulesAsync(["org/app"], [1000], [2000], []);

        Assert.Empty(result);
        _prpMock.Verify(p => p.GetPolicyVersionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRulesAsync_GrantDelegation_ReturnsRulesFromPolicy()
    {
        var delegation = new DelegationChange
        {
            DelegationChangeType = DelegationChangeType.Grant,
            OfferedByPartyId = 1000,
            PerformedByUserId = 42,
            BlobStoragePolicyPath = "org/app/delegations/p1.xml",
            BlobStorageVersionId = "v1"
        };

        _delegationRepoMock
            .Setup(r => r.GetAllCurrentDelegationChanges(It.IsAny<List<int>>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync([delegation]);

        var policy = CreatePolicyWithPermitRule("rule1",
            actionId: "read",
            subjectId: XacmlRequestAttribute.UserAttribute, subjectValue: "2000",
            resourceId: "urn:altinn:org", resourceValue: "org");

        _prpMock
            .Setup(p => p.GetPolicyVersionAsync("org/app/delegations/p1.xml", "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var sut = CreateSut();
        var result = await sut.GetRulesAsync(["org/app"], [1000], [2000], []);

        Assert.Single(result);
        var rule = result[0];
        Assert.Equal("rule1", rule.RuleId);
        Assert.Equal(1000, rule.OfferedByPartyId);
        Assert.Equal(42, rule.DelegatedByUserId);
        Assert.Equal("read", rule.Action.Value);
        Assert.Single(rule.CoveredBy);
        Assert.Equal("2000", rule.CoveredBy[0].Value);
        Assert.Single(rule.Resource);
        Assert.Equal("org", rule.Resource[0].Value);
    }

    [Fact]
    public async Task GetRulesAsync_DenyRule_IsExcluded()
    {
        var delegation = new DelegationChange
        {
            DelegationChangeType = DelegationChangeType.Grant,
            OfferedByPartyId = 1000,
            BlobStoragePolicyPath = "path",
            BlobStorageVersionId = "v1"
        };

        _delegationRepoMock
            .Setup(r => r.GetAllCurrentDelegationChanges(It.IsAny<List<int>>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync([delegation]);

        var denyRule = new XacmlRule("deny1", XacmlEffectType.Deny) { Target = CreateTarget("read", null, null, null, null) };
        var policy = CreateEmptyPolicy();
        policy.Rules.Add(denyRule);

        _prpMock
            .Setup(p => p.GetPolicyVersionAsync("path", "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var sut = CreateSut();
        var result = await sut.GetRulesAsync(["org/app"], [1000], [2000], []);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRulesAsync_RuleWithNullTarget_IsExcluded()
    {
        var delegation = new DelegationChange
        {
            DelegationChangeType = DelegationChangeType.Grant,
            OfferedByPartyId = 1000,
            BlobStoragePolicyPath = "path",
            BlobStorageVersionId = "v1"
        };

        _delegationRepoMock
            .Setup(r => r.GetAllCurrentDelegationChanges(It.IsAny<List<int>>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync([delegation]);

        var rule = new XacmlRule("r1", XacmlEffectType.Permit) { Target = null };
        var policy = CreateEmptyPolicy();
        policy.Rules.Add(rule);

        _prpMock
            .Setup(p => p.GetPolicyVersionAsync("path", "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var sut = CreateSut();
        var result = await sut.GetRulesAsync(["org/app"], [1000], [2000], []);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRulesAsync_MultipleDelegations_MixedTypes_ReturnsOnlyGrantRules()
    {
        var grant = new DelegationChange
        {
            DelegationChangeType = DelegationChangeType.Grant,
            OfferedByPartyId = 1000,
            PerformedByUserId = 1,
            BlobStoragePolicyPath = "path1",
            BlobStorageVersionId = "v1"
        };

        var revoke = new DelegationChange
        {
            DelegationChangeType = DelegationChangeType.RevokeLast,
            OfferedByPartyId = 2000,
            BlobStoragePolicyPath = "path2",
            BlobStorageVersionId = "v2"
        };

        _delegationRepoMock
            .Setup(r => r.GetAllCurrentDelegationChanges(It.IsAny<List<int>>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync([grant, revoke]);

        var policy = CreatePolicyWithPermitRule("rule1",
            actionId: "write",
            subjectId: XacmlRequestAttribute.PartyAttribute, subjectValue: "3000",
            resourceId: "urn:altinn:app", resourceValue: "app1");

        _prpMock
            .Setup(p => p.GetPolicyVersionAsync("path1", "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var sut = CreateSut();
        var result = await sut.GetRulesAsync(["org/app"], [1000, 2000], [3000], []);

        Assert.Single(result);
        Assert.Equal("write", result[0].Action.Value);
        _prpMock.Verify(p => p.GetPolicyVersionAsync("path2", "v2", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRulesAsync_MultiplePermitRules_ReturnsAll()
    {
        var delegation = new DelegationChange
        {
            DelegationChangeType = DelegationChangeType.Grant,
            OfferedByPartyId = 1000,
            PerformedByUserId = 1,
            BlobStoragePolicyPath = "path",
            BlobStorageVersionId = "v1"
        };

        _delegationRepoMock
            .Setup(r => r.GetAllCurrentDelegationChanges(It.IsAny<List<int>>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync([delegation]);

        var policy = CreateEmptyPolicy();
        policy.Rules.Add(CreatePermitRule("r1", "read", XacmlRequestAttribute.UserAttribute, "100", "urn:altinn:org", "org"));
        policy.Rules.Add(CreatePermitRule("r2", "write", XacmlRequestAttribute.UserAttribute, "100", "urn:altinn:org", "org"));

        _prpMock
            .Setup(p => p.GetPolicyVersionAsync("path", "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var sut = CreateSut();
        var result = await sut.GetRulesAsync(["org/app"], [1000], [], [100]);

        Assert.Equal(2, result.Count);
        Assert.Equal("r1", result[0].RuleId);
        Assert.Equal("r2", result[1].RuleId);
    }

    #region Helpers

    private static XacmlPolicy CreateEmptyPolicy() =>
        new(new Uri("urn:policy"), new Uri("urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"), new XacmlTarget(new List<XacmlAnyOf>()));

    private static XacmlPolicy CreatePolicyWithPermitRule(
        string ruleId,
        string actionId,
        string subjectId, string subjectValue,
        string resourceId, string resourceValue)
    {
        var policy = CreateEmptyPolicy();
        policy.Rules.Add(CreatePermitRule(ruleId, actionId, subjectId, subjectValue, resourceId, resourceValue));
        return policy;
    }

    private static XacmlRule CreatePermitRule(
        string ruleId,
        string actionId,
        string subjectId, string subjectValue,
        string resourceId, string resourceValue)
    {
        var target = CreateTarget(actionId, subjectId, subjectValue, resourceId, resourceValue);
        return new XacmlRule(ruleId, XacmlEffectType.Permit) { Target = target };
    }

    private static XacmlTarget CreateTarget(
        string actionId,
        string subjectId, string subjectValue,
        string resourceId, string resourceValue)
    {
        var target = new XacmlTarget(new List<XacmlAnyOf>());

        if (actionId != null)
        {
            var actionMatch = new XacmlMatch(
                new Uri(XacmlConstants.AttributeMatchFunction.StringEqual),
                new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), actionId),
                new XacmlAttributeDesignator(
                    new Uri(XacmlConstants.MatchAttributeCategory.Action),
                    new Uri(XacmlConstants.MatchAttributeIdentifiers.ActionId),
                    new Uri(XacmlConstants.DataTypes.XMLString),
                    false));
            target.AnyOf.Add(new XacmlAnyOf(new[] { new XacmlAllOf(new[] { actionMatch }) }));
        }

        if (subjectId != null)
        {
            var subjectMatch = new XacmlMatch(
                new Uri(XacmlConstants.AttributeMatchFunction.StringEqual),
                new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), subjectValue),
                new XacmlAttributeDesignator(
                    new Uri(XacmlConstants.MatchAttributeCategory.Subject),
                    new Uri(subjectId),
                    new Uri(XacmlConstants.DataTypes.XMLString),
                    false));
            target.AnyOf.Add(new XacmlAnyOf(new[] { new XacmlAllOf(new[] { subjectMatch }) }));
        }

        if (resourceId != null)
        {
            var resourceMatch = new XacmlMatch(
                new Uri(XacmlConstants.AttributeMatchFunction.StringEqual),
                new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), resourceValue),
                new XacmlAttributeDesignator(
                    new Uri(XacmlConstants.MatchAttributeCategory.Resource),
                    new Uri(resourceId),
                    new Uri(XacmlConstants.DataTypes.XMLString),
                    false));
            target.AnyOf.Add(new XacmlAnyOf(new[] { new XacmlAllOf(new[] { resourceMatch }) }));
        }

        return target;
    }

    #endregion
}
