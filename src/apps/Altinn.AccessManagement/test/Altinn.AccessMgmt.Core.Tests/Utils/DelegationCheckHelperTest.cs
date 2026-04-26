using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums.ResourceRegistry;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.AccessMgmt.Core.Tests.Utils;

/// <summary>
/// Pure unit tests for <see cref="DelegationCheckHelper"/>.
/// </summary>
public class DelegationCheckHelperTest
{
    // ── IsAccessListModeEnabledAndApplicable ─────────────────────────────────

    [Fact]
    public void IsAccessListModeEnabledAndApplicable_EnabledAndOrg_ReturnsTrue()
    {
        var result = DelegationCheckHelper.IsAccessListModeEnabledAndApplicable(
            ResourceAccessListMode.Enabled,
            EntityTypeConstants.Organization.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsAccessListModeEnabledAndApplicable_DisabledAndOrg_ReturnsFalse()
    {
        var result = DelegationCheckHelper.IsAccessListModeEnabledAndApplicable(
            ResourceAccessListMode.Disabled,
            EntityTypeConstants.Organization.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsAccessListModeEnabledAndApplicable_EnabledAndPerson_ReturnsFalse()
    {
        var result = DelegationCheckHelper.IsAccessListModeEnabledAndApplicable(
            ResourceAccessListMode.Enabled,
            EntityTypeConstants.Person.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsAccessListModeEnabledAndApplicable_DisabledAndPerson_ReturnsFalse()
    {
        var result = DelegationCheckHelper.IsAccessListModeEnabledAndApplicable(
            ResourceAccessListMode.Disabled,
            EntityTypeConstants.Person.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsAccessListModeEnabledAndApplicable_EnabledAndEmptyGuid_ReturnsFalse()
    {
        var result = DelegationCheckHelper.IsAccessListModeEnabledAndApplicable(
            ResourceAccessListMode.Enabled,
            Guid.Empty);

        result.Should().BeFalse();
    }

    // ── XACML helpers ─────────────────────────────────────────────────────────

    private static XacmlMatch MakeXacmlMatch(string category, string attributeId, string value) =>
        new(
            new Uri(XacmlConstants.AttributeMatchFunction.StringEqualIgnoreCase),
            new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), value),
            new XacmlAttributeDesignator(
                new Uri(category),
                new Uri(attributeId),
                new Uri(XacmlConstants.DataTypes.XMLString),
                false));

    /// <summary>
    /// Builds a rule with separate AnyOfs for subject, resource(s), and action —
    /// matching the structure produced by <c>BuildDelegationRuleTarget</c>.
    /// </summary>
    private static XacmlRule MakeRule(
        (string attrId, string value)? subject = null,
        IEnumerable<(string attrId, string value)>? resources = null,
        (string attrId, string value)? action = null)
    {
        var anyOfs = new List<XacmlAnyOf>();

        if (subject.HasValue)
        {
            anyOfs.Add(new XacmlAnyOf([new XacmlAllOf([MakeXacmlMatch(
                XacmlConstants.MatchAttributeCategory.Subject,
                subject.Value.attrId,
                subject.Value.value)])]));  
        }

        if (resources != null)
        {
            var matches = resources.Select(r => MakeXacmlMatch(
                XacmlConstants.MatchAttributeCategory.Resource, r.attrId, r.value));
            anyOfs.Add(new XacmlAnyOf([new XacmlAllOf(matches)]));
        }

        if (action.HasValue)
        {
            anyOfs.Add(new XacmlAnyOf([new XacmlAllOf([MakeXacmlMatch(
                XacmlConstants.MatchAttributeCategory.Action,
                action.Value.attrId,
                action.Value.value)])]));
        }

        return new XacmlRule("rule-1", XacmlEffectType.Permit)
        {
            Target = new XacmlTarget(anyOfs)
        };
    }

    private static XacmlPolicy MakePolicy(params XacmlRule[] rules)
    {
        var policy = new XacmlPolicy(
            new Uri("urn:test:policy"),
            new Uri("urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"),
            new XacmlTarget([]));
        foreach (var rule in rules)
        {
            policy.Rules.Add(rule);
        }

        return policy;
    }

    // ── GetFirstAccessorValuesFromPolicy ──────────────────────────────────────

    [Fact]
    public void GetFirstAccessorValuesFromPolicy_EmptyTarget_ReturnsEmpty()
    {
        var rule = new XacmlRule("r", XacmlEffectType.Permit)
        {
            Target = new XacmlTarget([])
        };

        var result = DelegationCheckHelper.GetFirstAccessorValuesFromPolicy(
            rule, XacmlConstants.MatchAttributeCategory.Subject);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetFirstAccessorValuesFromPolicy_SingleRoleMatchInSubjectCategory_ReturnsFormattedValue()
    {
        var rule = MakeRule(
            subject: (AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, "dagl"));

        var result = DelegationCheckHelper.GetFirstAccessorValuesFromPolicy(
            rule, XacmlConstants.MatchAttributeCategory.Subject).ToList();

        result.Should().ContainSingle();
        result[0].Should().Be($"{AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute}:dagl");
    }

    [Fact]
    public void GetFirstAccessorValuesFromPolicy_TwoSubjectMatchesInOneAllOf_NotReturned()
    {
        // Two matches in a single AllOf → count != 1 → excluded
        var allOf = new XacmlAllOf(
        [
            MakeXacmlMatch(XacmlConstants.MatchAttributeCategory.Subject, AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, "dagl"),
            MakeXacmlMatch(XacmlConstants.MatchAttributeCategory.Subject, AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, "regn"),
        ]);
        var rule = new XacmlRule("r", XacmlEffectType.Permit)
        {
            Target = new XacmlTarget([new XacmlAnyOf([allOf])])
        };

        var result = DelegationCheckHelper.GetFirstAccessorValuesFromPolicy(
            rule, XacmlConstants.MatchAttributeCategory.Subject);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetFirstAccessorValuesFromPolicy_WrongCategory_ReturnsEmpty()
    {
        // Resource match only — querying Subject should return nothing
        var rule = MakeRule(
            resources: [(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "my-resource")]);

        var result = DelegationCheckHelper.GetFirstAccessorValuesFromPolicy(
            rule, XacmlConstants.MatchAttributeCategory.Subject);

        result.Should().BeEmpty();
    }

    // ── DecomposePolicy ────────────────────────────────────────────────────────

    [Fact]
    public void DecomposePolicy_MatchingResourceAndRoleSubject_ReturnsOneRight()
    {
        var rule = MakeRule(
            subject: (AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, "dagl"),
            resources: [(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "my-resource-id")],
            action: ("urn:oasis:names:tc:xacml:1.0:action:action-id", "read"));
        var policy = MakePolicy(rule);

        var result = DelegationCheckHelper.DecomposePolicy(policy, "my-resource-id");

        result.Should().ContainSingle();
        result[0].AccessorUrns.Should().ContainSingle(u =>
            u == $"{AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute}:dagl");
    }

    [Fact]
    public void DecomposePolicy_NonMatchingResource_ReturnsEmpty()
    {
        var rule = MakeRule(
            subject: (AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, "dagl"),
            resources: [(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "other-resource")],
            action: ("urn:oasis:names:tc:xacml:1.0:action:action-id", "read"));
        var policy = MakePolicy(rule);

        var result = DelegationCheckHelper.DecomposePolicy(policy, "my-resource-id");

        result.Should().BeEmpty();
    }

    [Fact]
    public void DecomposePolicy_NonUserSubjectFiltered_RightNotAdded()
    {
        // PartyUuidAttribute is not a user-rule prefix → filtered by RemoveNonUserRules
        var rule = MakeRule(
            subject: (AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute, "some-party-uuid"),
            resources: [(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "my-resource-id")],
            action: ("urn:oasis:names:tc:xacml:1.0:action:action-id", "read"));
        var policy = MakePolicy(rule);

        var result = DelegationCheckHelper.DecomposePolicy(policy, "my-resource-id");

        result.Should().BeEmpty();
    }

    [Fact]
    public void DecomposePolicy_AccessPackageSubject_Included()
    {
        var rule = MakeRule(
            subject: (AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute, "altinn:pkg:test"),
            resources: [(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "my-resource-id")],
            action: ("urn:oasis:names:tc:xacml:1.0:action:action-id", "write"));
        var policy = MakePolicy(rule);

        var result = DelegationCheckHelper.DecomposePolicy(policy, "my-resource-id");

        result.Should().ContainSingle();
        result[0].AccessorUrns.Should().ContainSingle(u =>
            u.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute));
    }

    // ── BuildDelegationRuleTarget ──────────────────────────────────────────────

    [Fact]
    public void BuildDelegationRuleTarget_ReturnsTargetWithThreeAnyOfs()
    {
        var target = DelegationCheckHelper.BuildDelegationRuleTarget(
            "party-uuid-value",
            ["urn:altinn:resource:my-resource-id"],
            "urn:oasis:names:tc:xacml:1.0:action:action-id:write");

        target.Should().NotBeNull();
        target.AnyOf.Should().HaveCount(3);
    }

    [Fact]
    public void BuildDelegationRuleTarget_SubjectAnyOf_ContainsPartyUuidWithToId()
    {
        const string toId = "party-uuid-value";

        var target = DelegationCheckHelper.BuildDelegationRuleTarget(
            toId,
            ["urn:altinn:resource:my-resource-id"],
            "urn:oasis:names:tc:xacml:1.0:action:action-id:write");

        var subjectAnyOf = target.AnyOf.First();
        var match = subjectAnyOf.AllOf.First().Matches.First();
        match.AttributeValue.Value.Should().Be(toId);
        match.AttributeDesignator.AttributeId.OriginalString
            .Should().Be(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute);
    }

    [Fact]
    public void BuildDelegationRuleTarget_ResourceAnyOf_ContainsOneMatchPerResourceListEntry()
    {
        var target = DelegationCheckHelper.BuildDelegationRuleTarget(
            "party-uuid",
            ["urn:altinn:resource:res-1", "urn:altinn:task:task-1"],
            "urn:oasis:names:tc:xacml:1.0:action:action-id:read");

        // Second AnyOf = resource
        var resourceAnyOf = target.AnyOf.Skip(1).First();
        resourceAnyOf.AllOf.First().Matches.Should().HaveCount(2);
    }

    // ── CalculateRightKeys ────────────────────────────────────────────────────

    [Fact]
    public void CalculateRightKeys_RegularResource_MatchingResourceId_ReturnsHashedKey()
    {
        var rule = MakeRule(
            resources: [(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "my-resource-id")],
            action: ("urn:oasis:names:tc:xacml:1.0:action:action-id", "read"));

        var keys = DelegationCheckHelper.CalculateRightKeys(rule, "my-resource-id").ToList();

        keys.Should().ContainSingle();
        keys[0].Should().StartWith("01");
        keys[0].Should().HaveLength(66); // "01" + 64 hex chars
    }

    [Fact]
    public void CalculateRightKeys_NonMatchingResourceId_ReturnsEmpty()
    {
        var rule = MakeRule(
            resources: [(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "other-resource")],
            action: ("urn:oasis:names:tc:xacml:1.0:action:action-id", "read"));

        var keys = DelegationCheckHelper.CalculateRightKeys(rule, "my-resource-id");

        keys.Should().BeEmpty();
    }

    [Fact]
    public void CalculateRightKeys_OrgAppResource_RewrittenAndMatchedAsAppResourceId()
    {
        // org + app in one AllOf → rewritten to app_ttd_myapp
        var orgMatch = MakeXacmlMatch(
            XacmlConstants.MatchAttributeCategory.Resource,
            AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd");
        var appMatch = MakeXacmlMatch(
            XacmlConstants.MatchAttributeCategory.Resource,
            AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, "myapp");
        var actionMatch = MakeXacmlMatch(
            XacmlConstants.MatchAttributeCategory.Action,
            "urn:oasis:names:tc:xacml:1.0:action:action-id", "write");

        var resourceAllOf = new XacmlAllOf([orgMatch, appMatch]);
        var actionAllOf   = new XacmlAllOf([actionMatch]);
        var rule = new XacmlRule("r", XacmlEffectType.Permit)
        {
            Target = new XacmlTarget(
            [
                new XacmlAnyOf([resourceAllOf]),
                new XacmlAnyOf([actionAllOf]),
            ])
        };

        var keys = DelegationCheckHelper.CalculateRightKeys(rule, "app_ttd_myapp").ToList();

        keys.Should().ContainSingle();
        keys[0].Should().StartWith("01");
    }

    // ── IsAppResource ──────────────────────────────────────────────────────────

    [Fact]
    public void IsAppResource_AppPrefixedThreePart_ReturnsTrueWithOrgAndApp()
    {
        var isApp = DelegationCheckHelper.IsAppResource("app_ttd_myapp", out var org, out var app);

        isApp.Should().BeTrue();
        org.Should().Be("ttd");
        app.Should().Be("myapp");
    }

    [Fact]
    public void IsAppResource_NonAppResource_ReturnsFalseWithNullOrgAndApp()
    {
        var isApp = DelegationCheckHelper.IsAppResource("regular-resource-id", out var org, out var app);

        isApp.Should().BeFalse();
        org.Should().BeNull();
        app.Should().BeNull();
    }

    [Fact]
    public void IsAppResource_AppPrefixOnlyTwoParts_ReturnsTrueButOrgAndAppNull()
    {
        var isApp = DelegationCheckHelper.IsAppResource("app_ttd", out var org, out var app);

        isApp.Should().BeTrue();
        org.Should().BeNull();
        app.Should().BeNull();
    }

    // ── CheckIfErrorShouldBePushedToErrorQueue ─────────────────────────────────

    [Fact]
    public void CheckIfErrorShouldBePushedToErrorQueue_ResourceNotFoundMessage_ReturnsTrue()
    {
        var ex = new Exception("Resource 'my-res' not found");

        DelegationCheckHelper.CheckIfErrorShouldBePushedToErrorQueue(ex).Should().BeTrue();
    }

    [Fact]
    public void CheckIfErrorShouldBePushedToErrorQueue_FkConstraintToId_ReturnsTrue()
    {
        var inner = new Exception(
            "23503: insert or update on table \"assignment\" violates foreign key constraint \"fk_assignment_entity_toid\"");
        var ex = new Exception("Outer", inner);

        DelegationCheckHelper.CheckIfErrorShouldBePushedToErrorQueue(ex).Should().BeTrue();
    }

    [Fact]
    public void CheckIfErrorShouldBePushedToErrorQueue_FkConstraintFromId_ReturnsTrue()
    {
        var inner = new Exception(
            "23503: insert or update on table \"assignment\" violates foreign key constraint \"fk_assignment_entity_fromid\"");
        var ex = new Exception("Outer", inner);

        DelegationCheckHelper.CheckIfErrorShouldBePushedToErrorQueue(ex).Should().BeTrue();
    }

    [Fact]
    public void CheckIfErrorShouldBePushedToErrorQueue_AuditFieldsRequired_ReturnsTrue()
    {
        var ex = new Exception("Audit fields are required.");

        DelegationCheckHelper.CheckIfErrorShouldBePushedToErrorQueue(ex).Should().BeTrue();
    }

    [Fact]
    public void CheckIfErrorShouldBePushedToErrorQueue_FailedToFindPolicyFile_ReturnsTrue()
    {
        var ex = new Exception("Failed to find original policy file: /some/path/policy.xml");

        DelegationCheckHelper.CheckIfErrorShouldBePushedToErrorQueue(ex).Should().BeTrue();
    }

    [Fact]
    public void CheckIfErrorShouldBePushedToErrorQueue_GenericException_ReturnsFalse()
    {
        var ex = new Exception("Something unexpected happened");

        DelegationCheckHelper.CheckIfErrorShouldBePushedToErrorQueue(ex).Should().BeFalse();
    }
}
