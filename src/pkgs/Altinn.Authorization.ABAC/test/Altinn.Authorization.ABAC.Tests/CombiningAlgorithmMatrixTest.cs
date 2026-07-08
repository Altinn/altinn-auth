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
/// Rows where the engine already produces the spec decision are active. Rows where it
/// does not are skipped with a reason naming the spec-required decision; un-skipping a
/// row is the acceptance gate for the #3132 fix that implements that algorithm.
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
        yield return Row("Permit,IndeterminateP", Permit, IndeterminateHandlingSkip("deny-overrides", "C.2", Permit));
        yield return Row("IndeterminateP,Deny", Deny, IndeterminateHandlingSkip("deny-overrides", "C.2", Deny));
        yield return Row("IndeterminateD,Deny", Deny, IndeterminateHandlingSkip("deny-overrides", "C.2", Deny));
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
        const string alg = "ordered-deny-overrides";
        yield return Row("Permit", Permit);
        yield return Row("NotApplicable", NotApplicable);
        yield return Row("NotApplicableByTarget", NotApplicable);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("IndeterminateD", Indeterminate); // spec: Indeterminate{D}
        yield return Row("IndeterminateP", Indeterminate); // spec: Indeterminate{P}
        yield return Row("IndeterminateD,Permit", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Permit,IndeterminateD", Indeterminate); // spec: Indeterminate{DP}
        yield return Row("Deny", Deny, NotImplementedSkip(alg, "C.3", Deny, NotApplicable));
        yield return Row("Deny,Permit", Deny, NotImplementedSkip(alg, "C.3", Deny, Permit) + SilentGrant);
        yield return Row("Permit,Deny", Deny, NotImplementedSkip(alg, "C.3", Deny, Permit) + SilentGrant);
        yield return Row("NotApplicable,Deny", Deny, NotImplementedSkip(alg, "C.3", Deny, NotApplicable));
        yield return Row("Permit,IndeterminateP", Permit, NotImplementedSkip(alg, "C.3", Permit, Indeterminate));
        yield return Row("IndeterminateP,Deny", Deny, NotImplementedSkip(alg, "C.3", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateP", Deny, NotImplementedSkip(alg, "C.3", Deny, Indeterminate));
        yield return Row("IndeterminateD,Deny", Deny, NotImplementedSkip(alg, "C.3", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateD", Deny, NotImplementedSkip(alg, "C.3", Deny, Indeterminate));
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
        const string alg = "permit-overrides";
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
        yield return Row("Deny", Deny, NotImplementedSkip(alg, "C.4", Deny, NotApplicable));
        yield return Row("NotApplicable,Deny", Deny, NotImplementedSkip(alg, "C.4", Deny, NotApplicable));
        yield return Row("IndeterminateD,Permit", Permit, NotImplementedSkip(alg, "C.4", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateD", Permit, NotImplementedSkip(alg, "C.4", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateP", Permit, NotImplementedSkip(alg, "C.4", Permit, Indeterminate));
        yield return Row("IndeterminateD,Deny", Deny, NotImplementedSkip(alg, "C.4", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateD", Deny, NotImplementedSkip(alg, "C.4", Deny, Indeterminate));
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
        const string alg = "ordered-permit-overrides";
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
        yield return Row("Deny", Deny, NotImplementedSkip(alg, "C.5", Deny, NotApplicable));
        yield return Row("NotApplicable,Deny", Deny, NotImplementedSkip(alg, "C.5", Deny, NotApplicable));
        yield return Row("IndeterminateD,Permit", Permit, NotImplementedSkip(alg, "C.5", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateD", Permit, NotImplementedSkip(alg, "C.5", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateP", Permit, NotImplementedSkip(alg, "C.5", Permit, Indeterminate));
        yield return Row("IndeterminateD,Deny", Deny, NotImplementedSkip(alg, "C.5", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateD", Deny, NotImplementedSkip(alg, "C.5", Deny, Indeterminate));
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
        const string alg = "deny-unless-permit";
        yield return Row("Permit", Permit);
        yield return Row("Deny,Permit", Permit);
        yield return Row("Permit,Deny", Permit);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("Deny", Deny, NotImplementedSkip(alg, "C.6", Deny, NotApplicable));
        yield return Row("NotApplicable", Deny, NotImplementedSkip(alg, "C.6", Deny, NotApplicable));
        yield return Row("NotApplicableByTarget", Deny, NotImplementedSkip(alg, "C.6", Deny, NotApplicable));
        yield return Row("NotApplicable,Deny", Deny, NotImplementedSkip(alg, "C.6", Deny, NotApplicable));
        yield return Row("IndeterminateD", Deny, NotImplementedSkip(alg, "C.6", Deny, Indeterminate));
        yield return Row("IndeterminateP", Deny, NotImplementedSkip(alg, "C.6", Deny, Indeterminate));
        yield return Row("IndeterminateD,Permit", Permit, NotImplementedSkip(alg, "C.6", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateD", Permit, NotImplementedSkip(alg, "C.6", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateP", Permit, NotImplementedSkip(alg, "C.6", Permit, Indeterminate));
        yield return Row("IndeterminateP,Deny", Deny, NotImplementedSkip(alg, "C.6", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateP", Deny, NotImplementedSkip(alg, "C.6", Deny, Indeterminate));
        yield return Row("IndeterminateD,Deny", Deny, NotImplementedSkip(alg, "C.6", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateD", Deny, NotImplementedSkip(alg, "C.6", Deny, Indeterminate));
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
        const string alg = "permit-unless-deny";
        yield return Row("Permit", Permit);
        yield return Row("NotApplicable,Permit", Permit);
        yield return Row("Deny", Deny, NotImplementedSkip(alg, "C.7", Deny, NotApplicable));
        yield return Row("NotApplicable", Permit, NotImplementedSkip(alg, "C.7", Permit, NotApplicable));
        yield return Row("NotApplicableByTarget", Permit, NotImplementedSkip(alg, "C.7", Permit, NotApplicable));
        yield return Row("Deny,Permit", Deny, NotImplementedSkip(alg, "C.7", Deny, Permit) + SilentGrant);
        yield return Row("Permit,Deny", Deny, NotImplementedSkip(alg, "C.7", Deny, Permit) + SilentGrant);
        yield return Row("NotApplicable,Deny", Deny, NotImplementedSkip(alg, "C.7", Deny, NotApplicable));
        yield return Row("IndeterminateD", Permit, NotImplementedSkip(alg, "C.7", Permit, Indeterminate));
        yield return Row("IndeterminateP", Permit, NotImplementedSkip(alg, "C.7", Permit, Indeterminate));
        yield return Row("IndeterminateD,Permit", Permit, NotImplementedSkip(alg, "C.7", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateD", Permit, NotImplementedSkip(alg, "C.7", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateP", Permit, NotImplementedSkip(alg, "C.7", Permit, Indeterminate));
        yield return Row("IndeterminateP,Deny", Deny, NotImplementedSkip(alg, "C.7", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateP", Deny, NotImplementedSkip(alg, "C.7", Deny, Indeterminate));
        yield return Row("IndeterminateD,Deny", Deny, NotImplementedSkip(alg, "C.7", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateD", Deny, NotImplementedSkip(alg, "C.7", Deny, Indeterminate));
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
        const string alg = "first-applicable";
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
        yield return Row("Deny", Deny, NotImplementedSkip(alg, "C.8", Deny, NotApplicable));
        yield return Row("Deny,Permit", Deny, NotImplementedSkip(alg, "C.8", Deny, Permit) + SilentGrant);
        yield return Row("NotApplicable,Deny", Deny, NotImplementedSkip(alg, "C.8", Deny, NotApplicable));
        yield return Row("Permit,IndeterminateD", Permit, NotImplementedSkip(alg, "C.8", Permit, Indeterminate));
        yield return Row("Permit,IndeterminateP", Permit, NotImplementedSkip(alg, "C.8", Permit, Indeterminate));
        yield return Row("Deny,IndeterminateP", Deny, NotImplementedSkip(alg, "C.8", Deny, Indeterminate));
        yield return Row("Deny,IndeterminateD", Deny, NotImplementedSkip(alg, "C.8", Deny, Indeterminate));
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
    // 1. A Policy that names it as RuleCombiningAlgId is invalid. The PDP must not
    //    fall through to some other combining behaviour; the safe, spec-consistent
    //    response to an unsupported combining algorithm is Indeterminate.
    // 2. The real C.9 decision table needs PolicySet evaluation (#3132, XACML-2),
    //    which the engine does not expose. The expected decisions are encoded in
    //    OnlyOneApplicablePolicySetMatrix; when PolicySet combining lands, replace
    //    the placeholder body with a policy-set evaluation and un-skip.
    ////////////////////////////////////////////////////////////////////////////////

    [Fact(Skip = "#3132: only-one-applicable is a policy-combining algorithm (XACML 3.0 C.9) and is not a valid RuleCombiningAlgId. The PDP must reject it with Indeterminate instead of combining rules; it currently combines permissively and returns Permit for a Deny+Permit policy." + SilentGrant)]
    public void Authorize_OnlyOneApplicableAsRuleCombiningAlgorithm_ReturnsIndeterminate()
    {
        Decide(XacmlConstants.CombiningAlgorithms.PolicyOnlyOneApplicable, "Deny,Permit").Decision.Should().Be(Indeterminate);
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
    // Skip reasons
    ////////////////////////////////////////////////////////////////////////////////

    private const string SilentGrant = " This cell is a silent grant: the engine permits a request the policy denies.";

    private static string NotImplementedSkip(string algorithm, string specSection, XacmlContextDecision specDecision, XacmlContextDecision currentDecision) =>
        $"#3132: {algorithm} is not implemented in the PDP. XACML 3.0 {specSection} requires {specDecision} for this rule mix; the engine returns {currentDecision}. Un-skip when {algorithm} lands.";

    private static string IndeterminateHandlingSkip(string algorithm, string specSection, XacmlContextDecision specDecision) =>
        $"#3132: {algorithm} must keep combining across Indeterminate children and return {specDecision} for this rule mix (XACML 3.0 {specSection}); the engine stops at the first erroring rule and returns Indeterminate. Un-skip with the Indeterminate{{D}}/{{P}}/{{DP}} work (XACML-3).";

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
