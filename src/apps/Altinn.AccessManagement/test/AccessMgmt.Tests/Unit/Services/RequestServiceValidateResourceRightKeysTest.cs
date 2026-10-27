using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.Authorization.ABAC.Xacml;
using Moq;

namespace Altinn.AccessManagement.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="RequestService.ValidateResourceRightKeys"/>.
///
/// Uses the policy of <c>ttd-migratedcorrespondence-4471-1</c> from the test data. Its first rule gives
/// read and subscribe to a role, so those keys are delegable. Its second rule gives write and subscribe
/// only to the service owner org, so the write key exists in the policy but cannot be delegated.
/// The keys are calculated from the policy, so the tests do not depend on hardcoded hashes.
/// </summary>
[UnitTest]
public class RequestServiceValidateResourceRightKeysTest
{
    private const string Resource = "ttd-migratedcorrespondence-4471-1";

    private readonly PolicyRetrievalPointMock _policyRetrievalPoint = new();

    // The database, settings and feature manager are not used when validating right keys.
    private RequestService CreateService() => new(db: null, appsettings: null, featureManager: null, policyRetrievalPoint: _policyRetrievalPoint);

    private async Task<(List<string> DelegableKeys, List<string> NonDelegableKeys)> GetPolicyKeys()
    {
        XacmlPolicy policy = await _policyRetrievalPoint.GetPolicyAsync(Resource, TestContext.Current.CancellationToken);
        var delegableKeys = DelegationCheckHelper.CalculateRightKeys(policy.Rules.ElementAt(0), Resource).ToList();
        var nonDelegableKeys = DelegationCheckHelper.CalculateRightKeys(policy.Rules.ElementAt(1), Resource).Except(delegableKeys).ToList();
        return (delegableKeys, nonDelegableKeys);
    }

    [Fact]
    public async Task ValidateResourceRightKeys_NoKeys_ReturnsNull()
    {
        var problem = await CreateService().ValidateResourceRightKeys(Resource, null, TestContext.Current.CancellationToken);

        Assert.Null(problem);
    }

    [Fact]
    public async Task ValidateResourceRightKeys_EmptyList_ReturnsNull()
    {
        var problem = await CreateService().ValidateResourceRightKeys(Resource, [], TestContext.Current.CancellationToken);

        Assert.Null(problem);
    }

    [Fact]
    public async Task ValidateResourceRightKeys_DelegableKeys_ReturnsNull()
    {
        var (delegableKeys, _) = await GetPolicyKeys();
        Assert.NotEmpty(delegableKeys);

        var problem = await CreateService().ValidateResourceRightKeys(Resource, delegableKeys, TestContext.Current.CancellationToken);

        Assert.Null(problem);
    }

    [Fact]
    public async Task ValidateResourceRightKeys_UnknownKey_ReturnsRightKeyNotFound()
    {
        var problem = await CreateService().ValidateResourceRightKeys(Resource, ["unknown-right-key"], TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        var error = Assert.Single(problem.Errors);
        Assert.Equal(ValidationErrors.RightKeyNotFound.ErrorCode, error.ErrorCode);
    }

    [Fact]
    public async Task ValidateResourceRightKeys_KeyOnlyGivenToServiceOwner_ReturnsRightKeyNotDelegable()
    {
        var (_, nonDelegableKeys) = await GetPolicyKeys();
        var writeKey = Assert.Single(nonDelegableKeys);

        var problem = await CreateService().ValidateResourceRightKeys(Resource, [writeKey], TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        var error = Assert.Single(problem.Errors);
        Assert.Equal(ValidationErrors.RightKeyNotDelegable.ErrorCode, error.ErrorCode);
    }

    [Fact]
    public async Task ValidateResourceRightKeys_MixedKeys_ReturnsOneErrorPerInvalidKeyPointingToItsIndex()
    {
        var (delegableKeys, nonDelegableKeys) = await GetPolicyKeys();

        var problem = await CreateService().ValidateResourceRightKeys(
            Resource,
            [delegableKeys[0], "unknown-right-key", nonDelegableKeys[0]],
            TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(2, problem.Errors.Length);
        Assert.Contains(problem.Errors, e => e.ErrorCode == ValidationErrors.RightKeyNotFound.ErrorCode && e.Paths.Any(p => p.EndsWith("/rightKeys/1")));
        Assert.Contains(problem.Errors, e => e.ErrorCode == ValidationErrors.RightKeyNotDelegable.ErrorCode && e.Paths.Any(p => p.EndsWith("/rightKeys/2")));
    }

    [Fact]
    public async Task ValidateResourceRightKeys_ResourceWithoutPolicy_ReturnsRightKeyNotFound()
    {
        var problem = await CreateService().ValidateResourceRightKeys("resource-without-policy", ["any-right-key"], TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        var error = Assert.Single(problem.Errors);
        Assert.Equal(ValidationErrors.RightKeyNotFound.ErrorCode, error.ErrorCode);
    }

    [Fact]
    public async Task ValidateResourceRightKeys_KeyOnlyInDenyRule_ReturnsRightKeyNotDelegable()
    {
        // The role rule of the test policy, turned into a Deny rule: its keys exist but are not given by any Permit rule.
        var (delegableKeys, _) = await GetPolicyKeys();
        XacmlPolicy original = await _policyRetrievalPoint.GetPolicyAsync(Resource, TestContext.Current.CancellationToken);
        var policy = new XacmlPolicy(original.PolicyId, original.RuleCombiningAlgId, original.Target);
        policy.Rules.Add(new XacmlRule("deny-role-rule", XacmlEffectType.Deny) { Target = original.Rules.ElementAt(0).Target });
        policy.Rules.Add(original.Rules.ElementAt(1));

        var policyRetrievalPoint = new Mock<IPolicyRetrievalPoint>();
        policyRetrievalPoint.Setup(p => p.GetPolicyAsync(Resource, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        var service = new RequestService(db: null, appsettings: null, featureManager: null, policyRetrievalPoint: policyRetrievalPoint.Object);

        var problem = await service.ValidateResourceRightKeys(Resource, [delegableKeys[0]], TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        var error = Assert.Single(problem.Errors);
        Assert.Equal(ValidationErrors.RightKeyNotDelegable.ErrorCode, error.ErrorCode);
        Assert.Equal(2, policy.Rules.Count);
    }
}
