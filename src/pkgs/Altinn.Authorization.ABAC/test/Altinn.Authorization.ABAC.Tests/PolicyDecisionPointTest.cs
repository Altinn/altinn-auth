using System.Xml;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.Authorization.ABAC.Tests;

/// <summary>
/// Engine-level tests for <see cref="PolicyDecisionPoint"/>. Each test drives a
/// crafted minimal XACML 3.0 policy + request through
/// <see cref="PolicyDecisionPoint.Authorize"/>, exercising the parser, the Xacml
/// object model, attribute matching, the deny-overrides rule-combining algorithm,
/// and the missing-required-attribute path together — the breadth the indirect
/// conformance suite covers, but localized and without a web host.
/// </summary>
[UnitTest]
public class PolicyDecisionPointTest
{
    private const string Ns = "urn:oasis:names:tc:xacml:3.0:core:schema:wd-17";
    private static readonly string DenyOverrides = XacmlConstants.CombiningAlgorithms.RuleDenyOverrides;

    [Fact]
    public void Authorize_SubjectResourceActionAllMatch_PermitRule_ReturnsPermit()
    {
        XacmlContextResult result = Decide(Policy("Permit", DenyOverrides), Request());
        result.Decision.Should().Be(XacmlContextDecision.Permit);
    }

    [Fact]
    public void Authorize_MatchingDenyRule_DenyOverrides_ReturnsDeny()
    {
        XacmlContextResult result = Decide(Policy("Deny", DenyOverrides), Request());
        result.Decision.Should().Be(XacmlContextDecision.Deny);
    }

    [Fact]
    public void Authorize_NoRuleMatchesResource_ReturnsNotApplicable()
    {
        // resource value is present but does not match the rule target -> no matching rule.
        XacmlContextResult result = Decide(Policy("Permit", DenyOverrides), Request(resource: "other-resource"));
        result.Decision.Should().Be(XacmlContextDecision.NotApplicable);
    }

    [Fact]
    public void Authorize_RequiredResourceAttributeMissing_ReturnsIndeterminate()
    {
        // resource-id is MustBePresent in the rule target but absent from the request.
        XacmlContextResult result = Decide(Policy("Permit", DenyOverrides), Request(resource: null));
        result.Decision.Should().Be(XacmlContextDecision.Indeterminate);
    }

    [Fact]
    public void Authorize_SubjectDoesNotMatch_ReturnsNotApplicable()
    {
        // resource + action match (rule applies) but the subject differs -> rule yields NotApplicable.
        XacmlContextResult result = Decide(Policy("Permit", DenyOverrides), Request(subject: "other-user"));
        result.Decision.Should().Be(XacmlContextDecision.NotApplicable);
    }

    private static XacmlContextResult Decide(string policyXml, string requestXml)
    {
        using XmlReader policyReader = XmlReader.Create(new StringReader(policyXml));
        XacmlPolicy policy = XacmlParser.ParseXacmlPolicy(policyReader);

        using XmlReader requestReader = XmlReader.Create(new StringReader(requestXml));
        XacmlContextRequest request = XacmlParser.ReadContextRequest(requestReader);

        return new PolicyDecisionPoint().Authorize(request, policy).Results.Single();
    }

    private static string Policy(string effect, string combiningAlg) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Policy xmlns=""{Ns}"" PolicyId=""urn:test:policy"" Version=""1.0"" RuleCombiningAlgId=""{combiningAlg}"">
  <Target />
  <Rule RuleId=""urn:test:rule"" Effect=""{effect}"">
    <Target>
      <AnyOf><AllOf>{Match("urn:oasis:names:tc:xacml:1.0:resource:resource-id", "urn:oasis:names:tc:xacml:3.0:attribute-category:resource", "resource1")}</AllOf></AnyOf>
      <AnyOf><AllOf>{Match("urn:oasis:names:tc:xacml:1.0:action:action-id", "urn:oasis:names:tc:xacml:3.0:attribute-category:action", "read")}</AllOf></AnyOf>
      <AnyOf><AllOf>{Match("urn:oasis:names:tc:xacml:1.0:subject:subject-id", "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject", "user1")}</AllOf></AnyOf>
    </Target>
  </Rule>
</Policy>";

    private static string Match(string attributeId, string category, string value) => $@"
        <Match MatchId=""urn:oasis:names:tc:xacml:1.0:function:string-equal"">
          <AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">{value}</AttributeValue>
          <AttributeDesignator AttributeId=""{attributeId}"" Category=""{category}"" DataType=""http://www.w3.org/2001/XMLSchema#string"" MustBePresent=""true""/>
        </Match>";

    private static string Request(string? subject = "user1", string? resource = "resource1", string? action = "read") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Request xmlns=""{Ns}"" ReturnPolicyIdList=""false"" CombinedDecision=""false"">{Attributes("urn:oasis:names:tc:xacml:1.0:subject-category:access-subject", "urn:oasis:names:tc:xacml:1.0:subject:subject-id", subject)}{Attributes("urn:oasis:names:tc:xacml:3.0:attribute-category:resource", "urn:oasis:names:tc:xacml:1.0:resource:resource-id", resource)}{Attributes("urn:oasis:names:tc:xacml:3.0:attribute-category:action", "urn:oasis:names:tc:xacml:1.0:action:action-id", action)}
</Request>";

    private static string Attributes(string category, string attributeId, string? value) => value is null ? string.Empty : $@"
  <Attributes Category=""{category}"">
    <Attribute AttributeId=""{attributeId}"" IncludeInResult=""false"">
      <AttributeValue DataType=""http://www.w3.org/2001/XMLSchema#string"">{value}</AttributeValue>
    </Attribute>
  </Attributes>";
}
