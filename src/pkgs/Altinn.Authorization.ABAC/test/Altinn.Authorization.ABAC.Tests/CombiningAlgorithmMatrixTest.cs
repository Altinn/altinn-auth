using System.Xml;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.Authorization.ABAC.Tests;

/// <summary>
/// Acceptance-test matrix for the XACML 3.0 combining algorithms (issue #3132).
///
/// Each theory covers one combining algorithm. Each row is one cell of the spec's
/// decision table: an ordered list of rule outcomes (the child decisions) and the
/// combined decision required by XACML 3.0 Core (OASIS Standard, 22 January 2013),
/// Appendix C: C.2 deny-overrides, C.3 ordered-deny-overrides, C.4 permit-overrides,
/// C.5 ordered-permit-overrides, C.6 deny-unless-permit, C.7 permit-unless-deny,
/// C.8 first-applicable, C.9 only-one-applicable.
///
/// Rule tokens name the decision the rule evaluates to for the standard request
/// (user1 / resource1 / read):
/// <list type="bullet">
/// <item><c>Permit</c> / <c>Deny</c>: applicable rule with that effect.</item>
/// <item><c>NotApplicable</c>: rule whose subject target does not match.</item>
/// <item><c>NotApplicableByTarget</c>: rule whose resource target does not match,
/// so the rule is not applicable to the request at all.</item>
/// <item><c>IndeterminateP</c> / <c>IndeterminateD</c>: rule with effect Permit / Deny
/// whose subject target references an attribute absent from the request with
/// MustBePresent=true, so rule evaluation errors. Per section 7.11 this is
/// Indeterminate{P} / Indeterminate{D}.</item>
/// </list>
///
/// The engine exposes a single <see cref="XacmlContextDecision.Indeterminate"/> value,
/// so rows whose spec result is an extended Indeterminate{D}/{P}/{DP} assert the
/// collapsed Indeterminate and note the extended value in a row comment. Refining
/// those assertions belongs to the Indeterminate split (#3132, XACML-3).
///
/// Rows the engine answers are active. A skipped row names the engine limitation that
/// keeps it from the spec decision, and lifting that limitation is what un-skips it.
/// </summary>
[UnitTest]
public class CombiningAlgorithmMatrixTest
{
    private const string Ns = "urn:oasis:names:tc:xacml:3.0:core:schema:wd-17";

    private const XacmlContextDecision Permit = XacmlContextDecision.Permit;
    private const XacmlContextDecision Deny = XacmlContextDecision.Deny;
    private const XacmlContextDecision NotApplicable = XacmlContextDecision.NotApplicable;
    private const XacmlContextDecision Indeterminate = XacmlContextDecision.Indeterminate;

    ////////////////////////////////////////////////////////////////////////////////
    // C.2 deny-overrides
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> DenyOverridesMatrix()
    {
        yield return Row("Permit", Permit);
        yield return Row("Deny", Deny);
        yield return Row("NotApplicable", NotApplicable);
        yield return Row("NotApplicableByTarget", NotApplicable);
        yield return Row("Deny,Permit", Deny);
        yield return Row("Permit,Deny", Deny);
        yield return Row("NotApplicable,Deny", Deny);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("IndeterminateD", Indeterminate); // spec: Indeterminate{D}
        yield return Row("IndeterminateP", Indeterminate); // spec: Indeterminate{P}
        yield return Row("IndeterminateD,Permit", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Permit,IndeterminateD", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Deny,IndeterminateP", Deny);
        yield return Row("Deny,IndeterminateD", Deny);
        yield return Row("Permit,IndeterminateP", Permit);
        yield return Row("IndeterminateP,Deny", Deny);
        yield return Row("IndeterminateD,Deny", Deny);
    }

    [Theory]
    [MemberData(nameof(DenyOverridesMatrix))]
    public void Authorize_DenyOverrides_ReturnsDecisionFromXacmlCombiningTable(string rules, XacmlContextDecision expected)
    {
        Decide(XacmlConstants.CombiningAlgorithms.RuleDenyOverrides, rules).Decision.Should().Be(expected);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // C.3 ordered-deny-overrides (same decision table as C.2; ordering constrains
    // obligation/advice collection, not the decision)
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> OrderedDenyOverridesMatrix()
    {
        yield return Row("Permit", Permit);
        yield return Row("NotApplicable", NotApplicable);
        yield return Row("NotApplicableByTarget", NotApplicable);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("IndeterminateD", Indeterminate); // spec: Indeterminate{D}
        yield return Row("IndeterminateP", Indeterminate); // spec: Indeterminate{P}
        yield return Row("IndeterminateD,Permit", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Permit,IndeterminateD", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Deny", Deny);
        yield return Row("Deny,Permit", Deny);
        yield return Row("Permit,Deny", Deny);
        yield return Row("NotApplicable,Deny", Deny);
        yield return Row("Permit,IndeterminateP", Permit);
        yield return Row("IndeterminateP,Deny", Deny);
        yield return Row("Deny,IndeterminateP", Deny);
        yield return Row("IndeterminateD,Deny", Deny);
        yield return Row("Deny,IndeterminateD", Deny);
    }

    [Theory]
    [MemberData(nameof(OrderedDenyOverridesMatrix))]
    public void Authorize_OrderedDenyOverrides_ReturnsDecisionFromXacmlCombiningTable(string rules, XacmlContextDecision expected)
    {
        Decide(XacmlConstants.CombiningAlgorithms.RuleOrderedDenyOverrides, rules).Decision.Should().Be(expected);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // C.4 permit-overrides
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> PermitOverridesMatrix()
    {
        yield return Row("Permit", Permit);
        yield return Row("NotApplicable", NotApplicable);
        yield return Row("NotApplicableByTarget", NotApplicable);
        yield return Row("Deny,Permit", Permit);
        yield return Row("Permit,Deny", Permit);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("IndeterminateD", Indeterminate); // spec: Indeterminate{D}
        yield return Row("IndeterminateP", Indeterminate); // spec: Indeterminate{P}
        yield return Row("IndeterminateP,Deny", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Deny,IndeterminateP", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Deny", Deny);
        yield return Row("NotApplicable,Deny", Deny);
        yield return Row("IndeterminateD,Permit", Permit);
        yield return Row("Permit,IndeterminateD", Permit);
        yield return Row("Permit,IndeterminateP", Permit);
        yield return Row("IndeterminateD,Deny", Deny);
        yield return Row("Deny,IndeterminateD", Deny);
    }

    [Theory]
    [MemberData(nameof(PermitOverridesMatrix))]
    public void Authorize_PermitOverrides_ReturnsDecisionFromXacmlCombiningTable(string rules, XacmlContextDecision expected)
    {
        Decide(XacmlConstants.CombiningAlgorithms.RulePermitOverrides, rules).Decision.Should().Be(expected);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // C.5 ordered-permit-overrides (same decision table as C.4)
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> OrderedPermitOverridesMatrix()
    {
        yield return Row("Permit", Permit);
        yield return Row("NotApplicable", NotApplicable);
        yield return Row("NotApplicableByTarget", NotApplicable);
        yield return Row("Deny,Permit", Permit);
        yield return Row("Permit,Deny", Permit);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("IndeterminateD", Indeterminate); // spec: Indeterminate{D}
        yield return Row("IndeterminateP", Indeterminate); // spec: Indeterminate{P}
        yield return Row("IndeterminateP,Deny", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Deny,IndeterminateP", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Deny", Deny);
        yield return Row("NotApplicable,Deny", Deny);
        yield return Row("IndeterminateD,Permit", Permit);
        yield return Row("Permit,IndeterminateD", Permit);
        yield return Row("Permit,IndeterminateP", Permit);
        yield return Row("IndeterminateD,Deny", Deny);
        yield return Row("Deny,IndeterminateD", Deny);
    }

    [Theory]
    [MemberData(nameof(OrderedPermitOverridesMatrix))]
    public void Authorize_OrderedPermitOverrides_ReturnsDecisionFromXacmlCombiningTable(string rules, XacmlContextDecision expected)
    {
        Decide(XacmlConstants.CombiningAlgorithms.RuleOrderedPermitOverrides, rules).Decision.Should().Be(expected);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // C.6 deny-unless-permit: Permit if any rule permits, otherwise Deny.
    // Never NotApplicable or Indeterminate.
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> DenyUnlessPermitMatrix()
    {
        yield return Row("Permit", Permit);
        yield return Row("Deny,Permit", Permit);
        yield return Row("Permit,Deny", Permit);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("Deny", Deny);
        yield return Row("NotApplicable", Deny);
        yield return Row("NotApplicableByTarget", Deny);
        yield return Row("NotApplicable,Deny", Deny);
        yield return Row("IndeterminateD", Deny);
        yield return Row("IndeterminateP", Deny);
        yield return Row("IndeterminateD,Permit", Permit);
        yield return Row("Permit,IndeterminateD", Permit);
        yield return Row("Permit,IndeterminateP", Permit);
        yield return Row("IndeterminateP,Deny", Deny);
        yield return Row("Deny,IndeterminateP", Deny);
        yield return Row("IndeterminateD,Deny", Deny);
        yield return Row("Deny,IndeterminateD", Deny);
    }

    [Theory]
    [MemberData(nameof(DenyUnlessPermitMatrix))]
    public void Authorize_DenyUnlessPermit_ReturnsDecisionFromXacmlCombiningTable(string rules, XacmlContextDecision expected)
    {
        Decide(XacmlConstants.CombiningAlgorithms.RuleDenyUnlessPermit, rules).Decision.Should().Be(expected);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // C.7 permit-unless-deny: Deny if any rule denies, otherwise Permit.
    // Never NotApplicable or Indeterminate.
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> PermitUnlessDenyMatrix()
    {
        yield return Row("Permit", Permit);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("Deny", Deny);
        yield return Row("NotApplicable", Permit);
        yield return Row("NotApplicableByTarget", Permit);
        yield return Row("Deny,Permit", Deny);
        yield return Row("Permit,Deny", Deny);
        yield return Row("NotApplicable,Deny", Deny);
        yield return Row("IndeterminateD", Permit);
        yield return Row("IndeterminateP", Permit);
        yield return Row("IndeterminateD,Permit", Permit);
        yield return Row("Permit,IndeterminateD", Permit);
        yield return Row("Permit,IndeterminateP", Permit);
        yield return Row("IndeterminateP,Deny", Deny);
        yield return Row("Deny,IndeterminateP", Deny);
        yield return Row("IndeterminateD,Deny", Deny);
        yield return Row("Deny,IndeterminateD", Deny);
    }

    [Theory]
    [MemberData(nameof(PermitUnlessDenyMatrix))]
    public void Authorize_PermitUnlessDeny_ReturnsDecisionFromXacmlCombiningTable(string rules, XacmlContextDecision expected)
    {
        Decide(XacmlConstants.CombiningAlgorithms.RulePermittUnlessDeny, rules).Decision.Should().Be(expected);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // C.8 first-applicable: the first rule that evaluates to Permit or Deny decides;
    // an erroring rule reached before that yields Indeterminate; all NotApplicable
    // yields NotApplicable.
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> FirstApplicableMatrix()
    {
        yield return Row("Permit", Permit);
        yield return Row("NotApplicable", NotApplicable);
        yield return Row("NotApplicableByTarget", NotApplicable);
        yield return Row("Permit,Deny", Permit);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("IndeterminateD", Indeterminate);
        yield return Row("IndeterminateP", Indeterminate);
        yield return Row("IndeterminateD,Permit", Indeterminate);
        yield return Row("IndeterminateP,Deny", Indeterminate);
        yield return Row("IndeterminateD,Deny", Indeterminate);
        yield return Row("Deny", Deny);
        yield return Row("Deny,Permit", Deny);
        yield return Row("NotApplicable,Deny", Deny);
        yield return Row("Permit,IndeterminateD", Permit);
        yield return Row("Permit,IndeterminateP", Permit);
        yield return Row("Deny,IndeterminateP", Deny);
        yield return Row("Deny,IndeterminateD", Deny);
    }

    [Theory]
    [MemberData(nameof(FirstApplicableMatrix))]
    public void Authorize_FirstApplicable_ReturnsDecisionFromXacmlCombiningTable(string rules, XacmlContextDecision expected)
    {
        Decide(XacmlConstants.CombiningAlgorithms.RuleFirstApplicable, rules).Decision.Should().Be(expected);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // C.9 only-one-applicable. This is a policy-combining algorithm: it combines the
    // results of policies inside a policy set, and XACML 3.0 does not define it for
    // rules. Two consequences for this engine:
    //
    // 1. A Policy that names it as RuleCombiningAlgId is invalid. The PDP does not
    //    fall through to some other combining behaviour; the safe, spec-consistent
    //    response to an unsupported combining algorithm is Indeterminate.
    // 2. The real C.9 decision table needs PolicySet evaluation (#3132, XACML-2),
    //    which the engine does not expose. The expected decisions are encoded in
    //    OnlyOneApplicablePolicySetMatrix; when PolicySet combining lands, replace
    //    the placeholder body with a policy-set evaluation and un-skip.
    ////////////////////////////////////////////////////////////////////////////////

    [Fact]
    public void Authorize_OnlyOneApplicableAsRuleCombiningAlgorithm_ReturnsIndeterminate()
    {
        Decide(XacmlConstants.CombiningAlgorithms.PolicyOnlyOneApplicable, "Deny,Permit").Decision.Should().Be(Indeterminate);
    }

    [Fact]
    public void Authorize_OnlyOneApplicableAsRuleCombiningAlgorithm_NoRuleReachedByTarget_ReturnsIndeterminate()
    {
        Decide(XacmlConstants.CombiningAlgorithms.PolicyOnlyOneApplicable, "NotApplicableByTarget").Decision.Should().Be(Indeterminate);
    }

    public static IEnumerable<TheoryDataRow<string, XacmlContextDecision>> OnlyOneApplicablePolicySetMatrix()
    {
        const string skip = "#3132: only-one-applicable requires PolicySet evaluation (XACML-2), which the engine does not expose. The expected decision per XACML 3.0 C.9 is encoded in the row; implement PolicySet combining, replace the placeholder body with a policy-set evaluation, and un-skip.";
        yield return Row("OneApplicablePolicy(Permit),OneNonApplicablePolicy", Permit, skip);
        yield return Row("OneApplicablePolicy(Deny),OneNonApplicablePolicy", Deny, skip);
        yield return Row("TwoApplicablePolicies", Indeterminate, skip);
        yield return Row("NoApplicablePolicies", NotApplicable, skip);
    }

    [Theory]
    [MemberData(nameof(OnlyOneApplicablePolicySetMatrix))]
    public void Authorize_OnlyOneApplicablePolicySet_ReturnsDecisionFromXacmlCombiningTable(string policySetScenario, XacmlContextDecision expected)
    {
        Assert.Fail($"PolicySet evaluation is required to assert that only-one-applicable returns {expected} for {policySetScenario} (XACML 3.0 C.9); see #3132 (XACML-2).");
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Algorithm identifier aliases. A table is reachable through its rule-combining
    // URN, its ordered variant and the matching policy-combining URN. Delegation
    // policies are generated with the policy-combining deny-overrides URN in
    // RuleCombiningAlgId, so that alias carries production traffic.
    ////////////////////////////////////////////////////////////////////////////////

    public static IEnumerable<TheoryDataRow<string, string, XacmlContextDecision>> CombiningAlgorithmAliasMatrix()
    {
        foreach (string alias in DenyOverridesAliases)
        {
            yield return new(alias, "Permit", Permit);
            yield return new(alias, "Deny,Permit", Deny);
        }

        foreach (string alias in PermitOverridesAliases)
        {
            yield return new(alias, "Deny", Deny);
            yield return new(alias, "Deny,Permit", Permit);
        }

        foreach (string alias in FirstApplicableAliases)
        {
            yield return new(alias, "Deny,Permit", Deny);
            yield return new(alias, "Permit,Deny", Permit);
        }

        foreach (string alias in DenyUnlessPermitAliases)
        {
            yield return new(alias, "Deny", Deny);
            yield return new(alias, "Deny,Permit", Permit);
        }

        foreach (string alias in PermitUnlessDenyAliases)
        {
            yield return new(alias, "Permit", Permit);
            yield return new(alias, "Deny,Permit", Deny);
        }
    }

    [Theory]
    [MemberData(nameof(CombiningAlgorithmAliasMatrix))]
    public void Authorize_CombiningAlgorithmAlias_ReturnsDecisionFromXacmlCombiningTable(string combiningAlgorithm, string rules, XacmlContextDecision expected)
    {
        Decide(combiningAlgorithm, rules).Decision.Should().Be(expected);
    }

    [Fact]
    public void Authorize_DelegationPolicyWithPolicyCombiningDenyOverrides_SinglePermitRule_ReturnsPermit()
    {
        Decide(XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides, "Permit").Decision.Should().Be(Permit);
    }

    [Fact]
    public void Authorize_DelegationPolicyWithPolicyCombiningDenyOverrides_DenyAndPermitRules_ReturnsDeny()
    {
        Decide(XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides, "Deny,Permit").Decision.Should().Be(Deny);
    }

    private static readonly string[] DenyOverridesAliases =
    [
        XacmlConstants.CombiningAlgorithms.RuleDenyOverrides,
        XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides,
        XacmlConstants.CombiningAlgorithms.RuleOrderedDenyOverrides,
        XacmlConstants.CombiningAlgorithms.PolicyOrderedDenyOverrided,
    ];

    private static readonly string[] PermitOverridesAliases =
    [
        XacmlConstants.CombiningAlgorithms.RulePermitOverrides,
        XacmlConstants.CombiningAlgorithms.PolicyPermidOverrides,
        XacmlConstants.CombiningAlgorithms.RuleOrderedPermitOverrides,
        XacmlConstants.CombiningAlgorithms.PolicyOrderedPermitOverrides,
    ];

    private static readonly string[] FirstApplicableAliases =
    [
        XacmlConstants.CombiningAlgorithms.RuleFirstApplicable,
        XacmlConstants.CombiningAlgorithms.PolicyFirstApplicable,
    ];

    private static readonly string[] DenyUnlessPermitAliases =
    [
        XacmlConstants.CombiningAlgorithms.RuleDenyUnlessPermit,
        XacmlConstants.CombiningAlgorithms.PolicyDenyUnlessPermit,
    ];

    private static readonly string[] PermitUnlessDenyAliases =
    [
        XacmlConstants.CombiningAlgorithms.RulePermittUnlessDeny,
        XacmlConstants.CombiningAlgorithms.PolicyPermitUnlessDeny,
    ];

    ////////////////////////////////////////////////////////////////////////////////
    // Harness: builds a minimal XACML 3.0 policy with one rule per token and runs it
    // through the PDP against the standard request.
    ////////////////////////////////////////////////////////////////////////////////

    private static TheoryDataRow<string, XacmlContextDecision> Row(string rules, XacmlContextDecision expected, string? skip = null) =>
        new(rules, expected) { Skip = skip };

    private static XacmlContextResult Decide(string combiningAlgorithm, string commaSeparatedRuleTokens)
    {
        string policyXml = PolicyXml(combiningAlgorithm, commaSeparatedRuleTokens.Split(','));

        using XmlReader policyReader = XmlReader.Create(new StringReader(policyXml));
        XacmlPolicy policy = XacmlParser.ParseXacmlPolicy(policyReader);

        using XmlReader requestReader = XmlReader.Create(new StringReader(RequestXml));
        XacmlContextRequest request = XacmlParser.ReadContextRequest(requestReader);

        return new PolicyDecisionPoint().Authorize(request, policy).Results.Single();
    }

    private static string PolicyXml(string combiningAlgorithm, IReadOnlyList<string> ruleTokens)
    {
        IEnumerable<string> rules = ruleTokens.Select(RuleXml);
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Policy xmlns=""{Ns}"" PolicyId=""urn:test:policy"" Version=""1.0"" RuleCombiningAlgId=""{combiningAlgorithm}"">
  <Target />
{string.Join("\n", rules)}
</Policy>";
    }

    private static string RuleXml(string token, int index)
    {
        (string effect, string resource, string subjectMatch) = token switch
        {
            "Permit" => ("Permit", "resource1", SubjectMatch("user1")),
            "Deny" => ("Deny", "resource1", SubjectMatch("user1")),
            "NotApplicable" => ("Permit", "resource1", SubjectMatch("someone-else")),
            "NotApplicableByTarget" => ("Permit", "resource2", SubjectMatch("user1")),
            "IndeterminateP" => ("Permit", "resource1", BrokenSubjectMatch),
            "IndeterminateD" => ("Deny", "resource1", BrokenSubjectMatch),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown rule token"),
        };

        return $@"  <Rule RuleId=""urn:test:rule{index}"" Effect=""{effect}"">
    <Target>
      <AnyOf><AllOf>{Match("urn:oasis:names:tc:xacml:1.0:resource:resource-id", "urn:oasis:names:tc:xacml:3.0:attribute-category:resource", resource)}</AllOf></AnyOf>
      <AnyOf><AllOf>{Match("urn:oasis:names:tc:xacml:1.0:action:action-id", "urn:oasis:names:tc:xacml:3.0:attribute-category:action", "read")}</AllOf></AnyOf>
      <AnyOf><AllOf>{subjectMatch}</AllOf></AnyOf>
    </Target>
  </Rule>";
    }

    private static string SubjectMatch(string value) =>
        Match("urn:oasis:names:tc:xacml:1.0:subject:subject-id", "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject", value);

    /// <summary>
    /// References an attribute id that is never present in the request, with
    /// MustBePresent=true, so target evaluation reports a missing required attribute
    /// and the rule evaluates to Indeterminate (with the rule's effect as the extended
    /// value per section 7.11).
    /// </summary>
    private static readonly string BrokenSubjectMatch =
        Match("urn:altinn:test:absent-attribute", "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject", "any-value");

    private static string Match(string attributeId, string category, string value) => $@"
        <Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
          <AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">{value}</AttributeValue>
          <AttributeDesignator AttributeId=""{attributeId}"" Category=""{category}"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""true""/>
        </Match>";

    private const string RequestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Request xmlns=""{Ns}"" ReturnPolicyIdList=""false"" CombinedDecision=""false"">
  <Attributes Category=""urn:oasis:names:tc:xacml:1.0:subject-category:access-subject"">
    <Attribute AttributeId=""urn:oasis:names:tc:xacml:1.0:subject:subject-id"" IncludeInResult=""false"">
      <AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">user1</AttributeValue>
    </Attribute>
  </Attributes>
  <Attributes Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:resource"">
    <Attribute AttributeId=""urn:oasis:names:tc:xacml:1.0:resource:resource-id"" IncludeInResult=""false"">
      <AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">resource1</AttributeValue>
    </Attribute>
  </Attributes>
  <Attributes Category=""urn:oasis:names:tc:xacml:3.0:attribute-category:action"">
    <Attribute AttributeId=""urn:oasis:names:tc:xacml:1.0:action:action-id"" IncludeInResult=""false"">
      <AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">read</AttributeValue>
    </Attribute>
  </Attributes>
</Request>";
}
