using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models;

namespace Altinn.Platform.Authorization.Tests;

public class PolicyHelperTest
{
    // --- GetAltinnAppsPolicyPath ---

    [Fact]
    public void GetAltinnAppsPolicyPath_ValidInput_ReturnsExpectedPath()
    {
        string result = PolicyHelper.GetAltinnAppsPolicyPath("ttd", "testapp");
        Assert.Equal("ttd/testapp/policy.xml", result);
    }

    [Fact]
    public void GetAltinnAppsPolicyPath_NullOrg_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PolicyHelper.GetAltinnAppsPolicyPath(null, "testapp"));
    }

    [Fact]
    public void GetAltinnAppsPolicyPath_EmptyApp_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PolicyHelper.GetAltinnAppsPolicyPath("ttd", ""));
    }

    // --- GetAltinnAppDelegationPolicyPath ---

    [Fact]
    public void GetAltinnAppDelegationPolicyPath_CoveredByPartyId_ReturnsPathWithPPrefix()
    {
        string result = PolicyHelper.GetAltinnAppDelegationPolicyPath("ttd", "testapp", "50001337", coveredByUserId: null, coveredByPartyId: 50001338);
        Assert.Equal("ttd/testapp/50001337/p50001338/delegationpolicy.xml", result);
    }

    [Fact]
    public void GetAltinnAppDelegationPolicyPath_CoveredByUserId_ReturnsPathWithUPrefix()
    {
        string result = PolicyHelper.GetAltinnAppDelegationPolicyPath("ttd", "testapp", "50001337", coveredByUserId: 20001337, coveredByPartyId: null);
        Assert.Equal("ttd/testapp/50001337/u20001337/delegationpolicy.xml", result);
    }

    [Fact]
    public void GetAltinnAppDelegationPolicyPath_NullOrg_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PolicyHelper.GetAltinnAppDelegationPolicyPath(null, "app", "1", coveredByUserId: 1, coveredByPartyId: null));
    }

    [Fact]
    public void GetAltinnAppDelegationPolicyPath_NullApp_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PolicyHelper.GetAltinnAppDelegationPolicyPath("org", null, "1", coveredByUserId: 1, coveredByPartyId: null));
    }

    [Fact]
    public void GetAltinnAppDelegationPolicyPath_NullOfferedBy_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PolicyHelper.GetAltinnAppDelegationPolicyPath("org", "app", null, coveredByUserId: 1, coveredByPartyId: null));
    }

    [Fact]
    public void GetAltinnAppDelegationPolicyPath_BothCoveredByNull_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PolicyHelper.GetAltinnAppDelegationPolicyPath("org", "app", "1", coveredByUserId: null, coveredByPartyId: null));
    }

    // --- GetPolicyResourceType ---

    [Fact]
    public void GetPolicyResourceType_OrgAndApp_ReturnsAltinnApps()
    {
        var request = CreateContextRequestWithResource(
            (AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd"),
            (AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, "testapp"));

        var result = PolicyHelper.GetPolicyResourceType(request, out string resourceId, out string org, out string app);

        Assert.Equal(PolicyResourceType.AltinnApps, result);
        Assert.Equal("ttd", org);
        Assert.Equal("testapp", app);
        Assert.Equal(string.Empty, resourceId);
    }

    [Fact]
    public void GetPolicyResourceType_ResourceRegistryNonApp_ReturnsResourceRegistry()
    {
        var request = CreateContextRequestWithResource(
            (AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistry, "nav_sykepenger"));

        var result = PolicyHelper.GetPolicyResourceType(request, out string resourceId, out string org, out string app);

        Assert.Equal(PolicyResourceType.ResourceRegistry, result);
        Assert.Equal("nav_sykepenger", resourceId);
    }

    [Fact]
    public void GetPolicyResourceType_ResourceRegistryAppPrefix_ReturnsAltinnApps()
    {
        var request = CreateContextRequestWithResource(
            (AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistry, "app_ttd_testapp"));

        var result = PolicyHelper.GetPolicyResourceType(request, out string resourceId, out string org, out string app);

        Assert.Equal(PolicyResourceType.AltinnApps, result);
        Assert.Equal("ttd", org);
        Assert.Equal("testapp", app);
    }

    [Fact]
    public void GetPolicyResourceType_NoResourceAttributes_ReturnsUndefined()
    {
        var request = new XacmlContextRequest(false, false, Array.Empty<XacmlContextAttributes>());

        var result = PolicyHelper.GetPolicyResourceType(request, out _, out _, out _);

        Assert.Equal(PolicyResourceType.Undefined, result);
    }

    // --- GetPolicyPath ---

    [Fact]
    public void GetPolicyPath_OrgAndApp_ReturnsExpected()
    {
        var request = CreateContextRequestWithResource(
            (AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd"),
            (AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, "testapp"));

        string result = PolicyHelper.GetPolicyPath(request);

        Assert.Equal("ttd/testapp/policy.xml", result);
    }

    // --- GetRolesWithAccess ---

    [Fact]
    public void GetRolesWithAccess_PolicyWithRoles_ReturnsUniqueRoles()
    {
        var policy = new XacmlPolicy(
            new Uri("urn:policy:1"), new Uri(XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides),
            new XacmlTarget(new List<XacmlAnyOf>()));

        var rule = new XacmlRule("rule1", XacmlEffectType.Permit)
        {
            Target = new XacmlTarget(new List<XacmlAnyOf>
            {
                new XacmlAnyOf(new List<XacmlAllOf>
                {
                    new XacmlAllOf(new List<XacmlMatch>
                    {
                        new XacmlMatch(
                            new Uri(XacmlConstants.AttributeMatchFunction.StringEqual),
                            new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), "REGNA"),
                            new XacmlAttributeDesignator(new Uri(XacmlConstants.MatchAttributeCategory.Subject), new Uri(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute), new Uri(XacmlConstants.DataTypes.XMLString), false))
                    })
                })
            })
        };

        policy.Rules.Add(rule);

        var roles = PolicyHelper.GetRolesWithAccess(policy);

        Assert.Single(roles);
        Assert.Contains("REGNA", roles);
    }

    [Fact]
    public void GetRolesWithAccess_DenyRule_ReturnsEmpty()
    {
        var policy = new XacmlPolicy(
            new Uri("urn:policy:1"), new Uri(XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides),
            new XacmlTarget(new List<XacmlAnyOf>()));

        var rule = new XacmlRule("rule1", XacmlEffectType.Deny)
        {
            Target = new XacmlTarget(new List<XacmlAnyOf>())
        };

        policy.Rules.Add(rule);

        var roles = PolicyHelper.GetRolesWithAccess(policy);

        Assert.Empty(roles);
    }

    // --- BuildDelegationPolicy ---

    [Fact]
    public void BuildDelegationPolicy_SingleRule_ReturnsPolicy()
    {
        var rules = new List<Rule>
        {
            new Rule
            {
                DelegatedByUserId = 20001,
                OfferedByPartyId = 50001,
                CoveredBy = new List<AttributeMatch> { new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = "20002" } },
                Resource = new List<AttributeMatch>
                {
                    new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, Value = "ttd" },
                    new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, Value = "app1" }
                },
                Action = new AttributeMatch { Id = XacmlConstants.MatchAttributeIdentifiers.ActionId, Value = "read" }
            }
        };

        var policy = PolicyHelper.BuildDelegationPolicy("ttd", "app1", 50001, coveredByPartyId: null, coveredByUserId: 20002, rules);

        Assert.NotNull(policy);
        Assert.Single(policy.Rules);
        Assert.Contains("ttd/app1", policy.Description);
    }

    // --- ParsePolicy / GetXmlMemoryStreamFromXacmlPolicy round-trip ---

    [Fact]
    public void ParsePolicy_RoundTrip_PreservesPolicy()
    {
        var original = PolicyHelper.BuildDelegationPolicy("ttd", "app1", 50001, coveredByPartyId: null, coveredByUserId: 20001, new List<Rule>
        {
            new Rule
            {
                DelegatedByUserId = 20001,
                Resource = new List<AttributeMatch>
                {
                    new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, Value = "ttd" },
                    new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, Value = "app1" }
                },
                Action = new AttributeMatch { Id = XacmlConstants.MatchAttributeIdentifiers.ActionId, Value = "write" }
            }
        });

        using var stream = PolicyHelper.GetXmlMemoryStreamFromXacmlPolicy(original);
        var parsed = PolicyHelper.ParsePolicy(stream);

        Assert.Single(parsed.Rules);
        Assert.Equal("1.0", parsed.Version);
    }

    // --- GetMinimumAuthenticationLevelFromXacmlPolicy ---

    [Fact]
    public void GetMinimumAuthenticationLevelFromXacmlPolicy_NoObligation_ReturnsZero()
    {
        var policy = new XacmlPolicy(
            new Uri("urn:policy:1"), new Uri(XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides),
            new XacmlTarget(new List<XacmlAnyOf>()));

        int result = PolicyHelper.GetMinimumAuthenticationLevelFromXacmlPolicy(policy);

        Assert.Equal(0, result);
    }

    // Helper

    private static XacmlContextRequest CreateContextRequestWithResource(params (string attributeId, string value)[] attributes)
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
