using Altinn.Authorization.ABAC.Constants;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.IntegrationTests.Data;
using Altinn.Platform.Authorization.Models;

namespace Altinn.Platform.Authorization.IntegrationTests;

/// <summary>
/// Additional unit tests for <see cref="DelegationHelper"/> methods not covered
/// by the existing <see cref="Altinn.Platform.Authorization.IntegrationTests.DelegationHelperTest"/>.
/// </summary>
public class DelegationHelperAdditionalTest
{
    // --- TryGetCoveredByPartyIdFromMatch ---

    [Fact]
    public void TryGetCoveredByPartyIdFromMatch_ValidPartyId_ReturnsTrue()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = "50001337" }
        };

        bool result = DelegationHelper.TryGetCoveredByPartyIdFromMatch(match, out int partyId);

        Assert.True(result);
        Assert.Equal(50001337, partyId);
    }

    [Fact]
    public void TryGetCoveredByPartyIdFromMatch_ZeroValue_ReturnsFalse()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = "0" }
        };

        bool result = DelegationHelper.TryGetCoveredByPartyIdFromMatch(match, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetCoveredByPartyIdFromMatch_WrongAttribute_ReturnsFalse()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "20001337" }
        };

        bool result = DelegationHelper.TryGetCoveredByPartyIdFromMatch(match, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetCoveredByPartyIdFromMatch_MultipleItems_ReturnsFalse()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = "1" },
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = "2" }
        };

        bool result = DelegationHelper.TryGetCoveredByPartyIdFromMatch(match, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetCoveredByPartyIdFromMatch_NullList_ReturnsFalse()
    {
        bool result = DelegationHelper.TryGetCoveredByPartyIdFromMatch(null, out _);
        Assert.False(result);
    }

    // --- TryGetCoveredByUserIdFromMatch ---

    [Fact]
    public void TryGetCoveredByUserIdFromMatch_ValidUserId_ReturnsTrue()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "20001337" }
        };

        bool result = DelegationHelper.TryGetCoveredByUserIdFromMatch(match, out int userId);

        Assert.True(result);
        Assert.Equal(20001337, userId);
    }

    [Fact]
    public void TryGetCoveredByUserIdFromMatch_ZeroValue_ReturnsFalse()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "0" }
        };

        bool result = DelegationHelper.TryGetCoveredByUserIdFromMatch(match, out _);

        Assert.False(result);
    }

    // --- GetCoveredByFromMatch ---

    [Fact]
    public void GetCoveredByFromMatch_ValidUserId_ReturnsUserIdString()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "20001337" }
        };

        string result = DelegationHelper.GetCoveredByFromMatch(match, out int? coveredByUserId, out int? coveredByPartyId);

        Assert.Equal("20001337", result);
        Assert.Equal(20001337, coveredByUserId);
        Assert.Null(coveredByPartyId);
    }

    [Fact]
    public void GetCoveredByFromMatch_ValidPartyId_ReturnsPartyIdString()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = "50001337" }
        };

        string result = DelegationHelper.GetCoveredByFromMatch(match, out int? coveredByUserId, out int? coveredByPartyId);

        Assert.Equal("50001337", result);
        Assert.Null(coveredByUserId);
        Assert.Equal(50001337, coveredByPartyId);
    }

    [Fact]
    public void GetCoveredByFromMatch_NoValidMatch_ReturnsNull()
    {
        var match = new List<AttributeMatch>
        {
            new AttributeMatch { Id = "urn:unknown", Value = "123" }
        };

        string result = DelegationHelper.GetCoveredByFromMatch(match, out int? coveredByUserId, out int? coveredByPartyId);

        Assert.Null(result);
        Assert.Null(coveredByUserId);
        Assert.Null(coveredByPartyId);
    }

    // --- TryGetResourceFromAttributeMatch ---

    [Fact]
    public void TryGetResourceFromAttributeMatch_ValidOrgApp_ReturnsTrue()
    {
        var input = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, Value = "ttd" },
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, Value = "testapp" }
        };

        bool result = DelegationHelper.TryGetResourceFromAttributeMatch(input, out string org, out string app);

        Assert.True(result);
        Assert.Equal("ttd", org);
        Assert.Equal("testapp", app);
    }

    [Fact]
    public void TryGetResourceFromAttributeMatch_MissingApp_ReturnsFalse()
    {
        var input = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, Value = "ttd" }
        };

        bool result = DelegationHelper.TryGetResourceFromAttributeMatch(input, out _, out _);

        Assert.False(result);
    }

    // --- GetAttributeMatchKey ---

    [Fact]
    public void GetAttributeMatchKey_SortsByIdAndConcatenates()
    {
        var matches = new List<AttributeMatch>
        {
            new AttributeMatch { Id = "urn:b", Value = "2" },
            new AttributeMatch { Id = "urn:a", Value = "1" }
        };

        string key = DelegationHelper.GetAttributeMatchKey(matches);

        Assert.Equal("urn:a1urn:b2", key);
    }

    // --- GetPolicyCount ---

    [Fact]
    public void GetPolicyCount_TwoDistinctPolicies_ReturnsTwo()
    {
        var rules = new List<Rule>
        {
            TestDataHelper.GetRuleModel(20001, 50001, "20002", AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "read", "org1", "app1"),
            TestDataHelper.GetRuleModel(20001, 50001, "20002", AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "read", "org1", "app2"),
        };

        int count = DelegationHelper.GetPolicyCount(rules);

        Assert.Equal(2, count);
    }

    [Fact]
    public void GetPolicyCount_SamePolicy_ReturnsOne()
    {
        var rules = new List<Rule>
        {
            TestDataHelper.GetRuleModel(20001, 50001, "20002", AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "read", "org1", "app1"),
            TestDataHelper.GetRuleModel(20001, 50001, "20002", AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "write", "org1", "app1"),
        };

        int count = DelegationHelper.GetPolicyCount(rules);

        Assert.Equal(1, count);
    }

    // --- GetRulesCountToDeleteFromRequestToDelete ---

    [Fact]
    public void GetRulesCountToDeleteFromRequestToDelete_ReturnsSum()
    {
        var requests = new List<RequestToDelete>
        {
            TestDataHelper.GetRequestToDeleteModel(20001, 50001, "org1", "app1", new List<string> { "r1", "r2" }, coveredByPartyId: 50002),
            TestDataHelper.GetRequestToDeleteModel(20001, 50001, "org1", "app2", new List<string> { "r3" }, coveredByPartyId: 50002),
        };

        int count = DelegationHelper.GetRulesCountToDeleteFromRequestToDelete(requests);

        Assert.Equal(3, count);
    }

    // --- TryGetDelegationParamsFromRule ---

    [Fact]
    public void TryGetDelegationParamsFromRule_ValidRule_ReturnsTrue()
    {
        var rule = TestDataHelper.GetRuleModel(20001, 50001, "50002", AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, "read", "org1", "app1");

        bool result = DelegationHelper.TryGetDelegationParamsFromRule(rule, out string org, out string app, out int offeredBy, out int? coveredByPartyId, out int? coveredByUserId, out int delegatedByUserId);

        Assert.True(result);
        Assert.Equal("org1", org);
        Assert.Equal("app1", app);
        Assert.Equal(50001, offeredBy);
        Assert.Equal(50002, coveredByPartyId);
        Assert.Null(coveredByUserId);
        Assert.Equal(20001, delegatedByUserId);
    }

    [Fact]
    public void TryGetDelegationParamsFromRule_MissingOrg_ReturnsFalse()
    {
        var rule = TestDataHelper.GetRuleModel(20001, 50001, "50002", AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, "read", null, "app1");

        bool result = DelegationHelper.TryGetDelegationParamsFromRule(rule, out _, out _, out _, out _, out _, out _);

        Assert.False(result);
    }

    // --- SetRuleType ---

    [Fact]
    public void SetRuleType_DirectlyDelegated_SetsCorrectType()
    {
        var rule = TestDataHelper.GetRuleModel(20001, 50001, "20002", AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "read", "org1", "app1");
        var coveredBy = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "20002" }
        };

        DelegationHelper.SetRuleType(new List<Rule> { rule }, offeredByPartyId: 50001, keyRolePartyIds: new List<int>(), coveredBy: coveredBy);

        Assert.Equal(RuleType.DirectlyDelegated, rule.Type);
    }

    [Fact]
    public void SetRuleType_InheritedViaKeyRole_SetsCorrectType()
    {
        // Rule delegated to party 50002 (a key role party) from 50001 — user requests via userId
        var rule = TestDataHelper.GetRuleModel(20001, 50001, "50002", AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, "read", "org1", "app1");
        var coveredBy = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "20002" }
        };

        DelegationHelper.SetRuleType(new List<Rule> { rule }, offeredByPartyId: 50001, keyRolePartyIds: new List<int> { 50002 }, coveredBy: coveredBy);

        Assert.Equal(RuleType.InheritedViaKeyRole, rule.Type);
    }

    [Fact]
    public void SetRuleType_InheritedAsSubunit_SetsCorrectType()
    {
        // Rule offered by parent (50000) to the user, requested for child (50001)
        var rule = TestDataHelper.GetRuleModel(20001, 50000, "20002", AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "read", "org1", "app1");
        var coveredBy = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "20002" }
        };

        DelegationHelper.SetRuleType(new List<Rule> { rule }, offeredByPartyId: 50001, keyRolePartyIds: new List<int>(), coveredBy: coveredBy, parentPartyId: 50000);

        Assert.Equal(RuleType.InheritedAsSubunit, rule.Type);
    }

    [Fact]
    public void SetRuleType_AlreadyTyped_DoesNotOverwrite()
    {
        var rule = TestDataHelper.GetRuleModel(20001, 50001, "20002", AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "read", "org1", "app1", ruleType: RuleType.DirectlyDelegated);
        var coveredBy = new List<AttributeMatch>
        {
            new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "99999" }
        };

        DelegationHelper.SetRuleType(new List<Rule> { rule }, offeredByPartyId: 50001, keyRolePartyIds: new List<int>(), coveredBy: coveredBy);

        // Rule already had a type so it should not have changed
        Assert.Equal(RuleType.DirectlyDelegated, rule.Type);
    }
}
