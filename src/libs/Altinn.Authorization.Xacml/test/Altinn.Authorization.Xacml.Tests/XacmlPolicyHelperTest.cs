using System.Text;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Xacml.Constants;

namespace Altinn.Authorization.Xacml.Tests;

[UnitTest]
public class XacmlPolicyHelperTest
{
    private const string PolicyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <xacml:Policy xmlns:xacml="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17" PolicyId="urn:altinn:example:policyid:1" Version="1.0" RuleCombiningAlgId="urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides">
          <xacml:Target />
          <xacml:Rule RuleId="urn:altinn:example:ruleid:1" Effect="Permit">
            <xacml:Target>
              <xacml:AnyOf>
                <xacml:AllOf>
                  <xacml:Match MatchId="urn:oasis:names:tc:xacml:3.0:function:string-equal-ignore-case">
                    <xacml:AttributeValue DataType="http://www.w3.org/2001/XMLSchema#string">DAGL</xacml:AttributeValue>
                    <xacml:AttributeDesignator AttributeId="urn:altinn:rolecode" Category="urn:oasis:names:tc:xacml:1.0:subject-category:access-subject" DataType="http://www.w3.org/2001/XMLSchema#string" MustBePresent="false" />
                  </xacml:Match>
                </xacml:AllOf>
              </xacml:AnyOf>
              <xacml:AnyOf>
                <xacml:AllOf>
                  <xacml:Match MatchId="urn:oasis:names:tc:xacml:1.0:function:string-equal">
                    <xacml:AttributeValue DataType="http://www.w3.org/2001/XMLSchema#string">read</xacml:AttributeValue>
                    <xacml:AttributeDesignator AttributeId="urn:oasis:names:tc:xacml:1.0:action:action-id" Category="urn:oasis:names:tc:xacml:3.0:attribute-category:action" DataType="http://www.w3.org/2001/XMLSchema#string" MustBePresent="false" />
                  </xacml:Match>
                </xacml:AllOf>
              </xacml:AnyOf>
            </xacml:Target>
          </xacml:Rule>
          <xacml:ObligationExpressions>
            <xacml:ObligationExpression FulfillOn="Permit" ObligationId="urn:altinn:obligation:authenticationLevel1">
              <xacml:AttributeAssignmentExpression AttributeId="urn:altinn:obligation-assignment:1" Category="urn:altinn:minimum-authenticationlevel">
                <xacml:AttributeValue DataType="http://www.w3.org/2001/XMLSchema#integer">3</xacml:AttributeValue>
              </xacml:AttributeAssignmentExpression>
            </xacml:ObligationExpression>
          </xacml:ObligationExpressions>
        </xacml:Policy>
        """;

    // --- GetAltinnAppsPolicyPath ---
    [Fact]
    public void GetAltinnAppsPolicyPath_ValidInput_ReturnsExpectedPath()
    {
        string result = XacmlPolicyHelper.GetAltinnAppsPolicyPath("ttd", "testapp");
        Assert.Equal("ttd/testapp/policy.xml", result);
    }

    [Fact]
    public void GetAltinnAppsPolicyPath_NullOrg_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => XacmlPolicyHelper.GetAltinnAppsPolicyPath(null, "testapp"));
    }

    [Fact]
    public void GetAltinnAppsPolicyPath_EmptyApp_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => XacmlPolicyHelper.GetAltinnAppsPolicyPath("ttd", string.Empty));
    }

    [Fact]
    public void GetAltinnAppsPolicyPath_InvalidFileNameCharacter_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => XacmlPolicyHelper.GetAltinnAppsPolicyPath("ttd", "test/app"));
    }

    // --- GetPolicyPath ---
    [Fact]
    public void GetPolicyPath_OrgAndApp_ReturnsExpected()
    {
        var request = CreateContextRequestWithResource(
            (AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd"),
            (AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, "testapp"));

        string result = XacmlPolicyHelper.GetPolicyPath(request);

        Assert.Equal("ttd/testapp/policy.xml", result);
    }

    [Fact]
    public void GetPolicyPath_MissingApp_ThrowsArgumentException()
    {
        var request = CreateContextRequestWithResource(
            (AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd"));

        Assert.Throws<ArgumentException>(() => XacmlPolicyHelper.GetPolicyPath(request));
    }

    // --- ParsePolicy / ParsePolicyAsync ---
    [Fact]
    public void ParsePolicy_StreamNotAtStart_ParsesFromStart()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(PolicyXml));
        stream.Position = stream.Length;

        XacmlPolicy policy = XacmlPolicyHelper.ParsePolicy(stream);

        Assert.Equal("urn:altinn:example:policyid:1", policy.PolicyId.OriginalString);
        Assert.Single(policy.Rules);
    }

    [Fact]
    public async Task ParsePolicyAsync_Stream_ParsesPolicy()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(PolicyXml));

        XacmlPolicy policy = await XacmlPolicyHelper.ParsePolicyAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal("urn:altinn:example:policyid:1", policy.PolicyId.OriginalString);
        Assert.Single(policy.Rules);
    }

    // --- GetMinimumAuthenticationLevelFromXacmlPolicy ---
    [Fact]
    public void GetMinimumAuthenticationLevelFromXacmlPolicy_NoObligation_ReturnsZero()
    {
        var policy = new XacmlPolicy(
            new Uri("urn:policy:1"), new Uri(XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides),
            new XacmlTarget(new List<XacmlAnyOf>()));

        int result = XacmlPolicyHelper.GetMinimumAuthenticationLevelFromXacmlPolicy(policy);

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetMinimumAuthenticationLevelFromXacmlPolicy_ObligationWithLevel_ReturnsLevel()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(PolicyXml));
        XacmlPolicy policy = XacmlPolicyHelper.ParsePolicy(stream);

        int result = XacmlPolicyHelper.GetMinimumAuthenticationLevelFromXacmlPolicy(policy);

        Assert.Equal(3, result);
    }

    // --- GetRulePolicyAttributeMatchesForCategory ---
    [Fact]
    public void GetRulePolicyAttributeMatchesForCategory_GroupsMatchesPerAllOfAndSkipsOtherCategories()
    {
        var rule = new XacmlRule("rule1", XacmlEffectType.Permit)
        {
            Target = new XacmlTarget(new List<XacmlAnyOf>
            {
                new XacmlAnyOf(new List<XacmlAllOf>
                {
                    new XacmlAllOf(new List<XacmlMatch>
                    {
                        CreateMatch(XacmlConstants.MatchAttributeCategory.Resource, AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd"),
                        CreateMatch(XacmlConstants.MatchAttributeCategory.Resource, AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, "testapp"),
                    }),
                    new XacmlAllOf(new List<XacmlMatch>
                    {
                        CreateMatch(XacmlConstants.MatchAttributeCategory.Resource, AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "resource1"),
                    }),
                }),
                new XacmlAnyOf(new List<XacmlAllOf>
                {
                    new XacmlAllOf(new List<XacmlMatch>
                    {
                        CreateMatch(XacmlConstants.MatchAttributeCategory.Action, XacmlConstants.MatchAttributeIdentifiers.ActionId, "read"),
                    }),
                }),
            })
        };

        List<List<(string Id, string Value)>> result = XacmlPolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Resource, (id, value) => (id, value));

        Assert.Equal(2, result.Count);
        Assert.Equal([(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd"), (AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, "testapp")], result[0]);
        Assert.Equal([(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute, "resource1")], result[1]);
    }

    [Fact]
    public void GetRulePolicyAttributeMatchesForCategory_NoMatchesInCategory_ReturnsEmpty()
    {
        var rule = new XacmlRule("rule1", XacmlEffectType.Permit)
        {
            Target = new XacmlTarget(new List<XacmlAnyOf>
            {
                new XacmlAnyOf(new List<XacmlAllOf>
                {
                    new XacmlAllOf(new List<XacmlMatch>
                    {
                        CreateMatch(XacmlConstants.MatchAttributeCategory.Action, XacmlConstants.MatchAttributeIdentifiers.ActionId, "read"),
                    }),
                }),
            })
        };

        var result = XacmlPolicyHelper.GetRulePolicyAttributeMatchesForCategory(rule, XacmlConstants.MatchAttributeCategory.Subject, (id, value) => (id, value));

        Assert.Empty(result);
    }

    private static XacmlMatch CreateMatch(string category, string attributeId, string value)
    {
        return new XacmlMatch(
            new Uri(XacmlConstants.AttributeMatchFunction.StringEqual),
            new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), value),
            new XacmlAttributeDesignator(new Uri(category), new Uri(attributeId), new Uri(XacmlConstants.DataTypes.XMLString), false));
    }

    private static XacmlContextRequest CreateContextRequestWithResource(params (string AttributeId, string Value)[] attributes)
    {
        var contextAttributes = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));

        foreach (var (attributeId, value) in attributes)
        {
            var attr = new XacmlAttribute(new Uri(attributeId), false);
            attr.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), value));
            contextAttributes.Attributes.Add(attr);
        }

        return new XacmlContextRequest(false, false, new[] { contextAttributes });
    }
}
