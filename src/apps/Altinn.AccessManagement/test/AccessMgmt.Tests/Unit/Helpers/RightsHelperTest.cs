using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.AccessList;

namespace Altinn.AccessManagement.Tests.Unit.Helpers;

/// <summary>
/// Pure-unit tests for <see cref="RightsHelper"/>. Covers the end-user-rule
/// classification and the delegation-access reason analysis: which
/// <see cref="DetailCode"/> each combination of right sources and access-list
/// result produces, the access-list-failure mutation of <c>CanDelegate</c>,
/// and the Unknown fallback.
/// </summary>
[UnitTest]
public class RightsHelperTest
{
    private const string RoleAttr = AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute;

    private static List<PolicyAttributeMatch> RoleSubject(string role = "DAGL") =>
        new() { new PolicyAttributeMatch { Id = RoleAttr, Value = role } };

    private static List<PolicyAttributeMatch> NonRoleSubject() =>
        new() { new PolicyAttributeMatch { Id = "urn:altinn:resource", Value = "some-resource" } };

    private static RightSource Source(RightSourceType type, bool? canDelegate, List<PolicyAttributeMatch> subject) =>
        new()
        {
            RightSourceType = type,
            CanDelegate = canDelegate,
            PolicySubjects = new List<List<PolicyAttributeMatch>> { subject },
        };

    private static Right Right(bool? canDelegate, params RightSource[] sources) =>
        new() { CanDelegate = canDelegate, RightSources = sources.ToList() };

    // ── CheckIfRuleIsAnEndUserRule ───────────────────────────────────────────
    [Fact]
    public void CheckIfRuleIsAnEndUserRule_NonDelegationSourceWithRoleSubject_ReturnsTrue()
    {
        var right = Right(true, Source(RightSourceType.ResourceRegistryPolicy, true, RoleSubject()));

        Assert.True(RightsHelper.CheckIfRuleIsAnEndUserRule(right));
    }

    [Fact]
    public void CheckIfRuleIsAnEndUserRule_OnlyDelegationPolicySources_ReturnsFalse()
    {
        var right = Right(true, Source(RightSourceType.DelegationPolicy, true, RoleSubject()));

        Assert.False(RightsHelper.CheckIfRuleIsAnEndUserRule(right));
    }

    [Fact]
    public void CheckIfRuleIsAnEndUserRule_NonDelegationSourceWithoutRoleSubject_ReturnsFalse()
    {
        var right = Right(true, Source(RightSourceType.ResourceRegistryPolicy, true, NonRoleSubject()));

        Assert.False(RightsHelper.CheckIfRuleIsAnEndUserRule(right));
    }

    [Fact]
    public void CheckIfRuleIsAnEndUserRule_NoSources_ReturnsFalse()
    {
        var right = Right(true);

        Assert.False(RightsHelper.CheckIfRuleIsAnEndUserRule(right));
    }

    // ── AnalyzeDelegationAccessReason: CanDelegate = true ────────────────────
    [Fact]
    public void AnalyzeDelegationAccessReason_CanDelegateWithRoleSource_ReturnsRoleAccess()
    {
        var right = Right(true, Source(RightSourceType.ResourceRegistryPolicy, true, RoleSubject()));

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right);

        Assert.Contains(reasons, d => d.Code == DetailCode.RoleAccess);
    }

    [Fact]
    public void AnalyzeDelegationAccessReason_CanDelegateWithDelegationPolicySource_ReturnsDelegationAccess()
    {
        var right = Right(true, Source(RightSourceType.DelegationPolicy, true, RoleSubject()));

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right);

        Assert.Contains(reasons, d => d.Code == DetailCode.DelegationAccess);
    }

    [Fact]
    public void AnalyzeDelegationAccessReason_AccessListAuthorized_AddsAccessListValidationPass()
    {
        var right = Right(true, Source(RightSourceType.ResourceRegistryPolicy, true, RoleSubject()));

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right, AccessListAuthorizationResult.Authorized);

        Assert.Contains(reasons, d => d.Code == DetailCode.AccessListValidationPass);
    }

    [Fact]
    public void AnalyzeDelegationAccessReason_AccessListNotAuthorized_AddsFailAndClearsCanDelegate()
    {
        var right = Right(true, Source(RightSourceType.ResourceRegistryPolicy, true, RoleSubject()));

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right, AccessListAuthorizationResult.NotAuthorized);

        Assert.Contains(reasons, d => d.Code == DetailCode.AccessListValidationFail);

        // Side effect: a failed access-list check flips CanDelegate to false.
        Assert.False(right.CanDelegate);
    }

    // ── AnalyzeDelegationAccessReason: CanDelegate = false ───────────────────
    [Fact]
    public void AnalyzeDelegationAccessReason_CannotDelegateWithRoleSource_ReturnsMissingRoleAccess()
    {
        var right = Right(false, Source(RightSourceType.ResourceRegistryPolicy, false, RoleSubject()));

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right);

        Assert.Contains(reasons, d => d.Code == DetailCode.MissingRoleAccess);
    }

    [Fact]
    public void AnalyzeDelegationAccessReason_CannotDelegateWithNoDelegationPolicySource_ReturnsMissingDelegationAccess()
    {
        var right = Right(false);

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right);

        Assert.Contains(reasons, d => d.Code == DetailCode.MissingDelegationAccess);
    }

    // ── AnalyzeDelegationAccessReason: Unknown fallback ──────────────────────
    [Fact]
    public void AnalyzeDelegationAccessReason_CanDelegateNull_ReturnsUnknown()
    {
        var right = Right(null);

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right);

        Assert.Single(reasons);
        Assert.Equal(DetailCode.Unknown, reasons[0].Code);
    }

    [Fact]
    public void AnalyzeDelegationAccessReason_CanDelegateTrueWithNoQualifyingSources_ReturnsUnknown()
    {
        var right = Right(true);

        var reasons = RightsHelper.AnalyzeDelegationAccessReason(right);

        Assert.Single(reasons);
        Assert.Equal(DetailCode.Unknown, reasons[0].Code);
    }
}
