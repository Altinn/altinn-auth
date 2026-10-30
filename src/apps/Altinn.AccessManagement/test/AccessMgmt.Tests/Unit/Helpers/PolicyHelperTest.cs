using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Enums;
using Altinn.AccessManagement.Tests.Utils;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.AccessManagement.Tests.Unit.Helpers
{
    /// <summary>
    /// Test class for <see cref="PolicyHelper"></see>
    /// </summary>
    [UnitTest]
    public class PolicyHelperTest
    {
        private readonly PolicyRetrievalPointMock _policyRetrievalPointMock;

        /// <summary>
        /// Constructor setting up dependencies
        /// </summary>
        public PolicyHelperTest()
        {
            _policyRetrievalPointMock = new PolicyRetrievalPointMock();
        }

        /// <summary>
        /// Scenario:
        /// Tests that the GetAltinnAppsPolicyPath method returns a correct path based on the input parameters.
        /// Input:
        /// Org and app from the AltinnAppId.
        /// Expected Result:
        /// True
        /// Success Criteria:
        /// Rule is found and expected result is returned
        /// </summary>
        [Fact]
        public void GetAltinnAppsPolicyPath_OrgAndApp_ReturnsExpectedPath()
        {
            // Arrange
            string expected = $"org1/app1/policy.xml";

            // Act
            string actual = PolicyHelper.GetAltinnAppsPolicyPath("org1", "app1");

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Scenario:
        /// Tests that the GetAltinnAppsPolicyPath method returns a correct path based on the input parameters.
        /// Input:
        /// Org and app from the AltinnAppId.
        /// Expected Result:
        /// True
        /// Success Criteria:
        /// Rule is found and expected result is returned
        /// </summary>
        /// <summary>
        /// Scenario:
        /// Tests that GetRolesWithAccess returns roles from both the urn:altinn:rolecode and the urn:altinn:role prefixes.
        /// Expected Result:
        /// Both role codes are returned
        /// </summary>
        [Fact]
        public void GetRolesWithAccess_RolecodeAndRolePrefixes_ReturnsRolesFromBoth()
        {
            // Arrange
            XacmlPolicy policy = new XacmlPolicy(
                new Uri("urn:policy:1"), new Uri(XacmlConstants.CombiningAlgorithms.PolicyDenyOverrides), new XacmlTarget(new List<XacmlAnyOf>()));

            policy.Rules.Add(new XacmlRule("rule1", XacmlEffectType.Permit)
            {
                Target = new XacmlTarget(new List<XacmlAnyOf>
                {
                    new XacmlAnyOf(new List<XacmlAllOf>
                    {
                        new XacmlAllOf(new List<XacmlMatch> { CreateSubjectMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.LegacyRoleAttribute, "DAGL") }),
                        new XacmlAllOf(new List<XacmlMatch> { CreateSubjectMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, "innbygger-konkursbo-administrator") })
                    })
                })
            });

            // Act
            List<string> actual = PolicyHelper.GetRolesWithAccess(policy);

            // Assert
            Assert.Equal(2, actual.Count);
            Assert.Contains("DAGL", actual);
            Assert.Contains("innbygger-konkursbo-administrator", actual);
        }

        [Fact]
        public async Task GetRolesWithAccess_FromPolicy_ReturnsExpectedRoles()
        {
            // Arrange
            XacmlPolicy policy = await _policyRetrievalPointMock.GetPolicyAsync("resource1", TestContext.Current.CancellationToken);

            List<string> expected = TestDataUtil.GetRolesWithAccess();

            // Act
            List<string> actual = PolicyHelper.GetRolesWithAccess(policy);

            // Assert
            Assert.Equal(actual, expected);
        }

        /// <summary>
        /// Scenario:
        /// Tests that the GetDelegationPolicyPath method throws the expected argument exception when org does not have a value
        /// Input:
        /// ResourceId is null because this is a org/app scenario but only app has been provided as part of the resource
        /// Expected Result:
        /// Argument exception thrown
        /// Success Criteria:
        /// Argument exception has the expected error message
        /// </summary>
        [Fact]
        public void GetDelegationPolicyPath_OrgEmpty_ThrowsArgumentException()
        {
            // Arrange
            string expectedArgumentException = "Org was not defined";

            // Act
            string actual = string.Empty;
            try
            {
                PolicyHelper.GetDelegationPolicyPath(ResourceAttributeMatchType.AltinnAppId, null, null, "app", "50001337", 20001337, null, null, UuidType.NotSpecified);
            }
            catch (System.ArgumentException argEx)
            {
                actual = argEx.Message;
            }

            // Assert
            Assert.Equal(expectedArgumentException, actual);
        }

        /// <summary>
        /// Scenario:
        /// Tests that the GetDelegationPolicyPath method throws the expected argument exception when app does not have a value
        /// Input:
        /// ResourceId is null because this is a org/app scenario but only org has been provided as part of the resource
        /// Expected Result:
        /// Argument exception thrown
        /// Success Criteria:
        /// Argument exception has the expected error message
        /// </summary>
        [Fact]
        public void GetDelegationPolicyPath_AppEmpty_ThrowsArgumentException()
        {
            // Arrange
            string expectedArgumentException = "App was not defined";

            // Act
            string actual = string.Empty;
            try
            {
                PolicyHelper.GetDelegationPolicyPath(ResourceAttributeMatchType.AltinnAppId, null, "org", string.Empty, "50001337", 20001337, null, null, UuidType.NotSpecified);
            }
            catch (System.ArgumentException argEx)
            {
                actual = argEx.Message;
            }

            // Assert
            Assert.Equal(expectedArgumentException, actual);
        }

        /// <summary>
        /// Scenario:
        /// Tests that the GetDelegationPolicyPath method throws the expected argument exception when resourceId does not have a value
        /// Input:
        /// Org and App is null because this is a resourceId scenario but resourceId has been provided as an empty string in the delegation
        /// Expected Result:
        /// Argument exception thrown
        /// Success Criteria:
        /// Argument exception has the expected error message
        /// </summary>
        [Fact]
        public void GetDelegationPolicyPath_ResourceIdEmpty_ThrowsArgumentException()
        {
            // Arrange
            string expectedArgumentException = "ResourceRegistryId was not defined";

            // Act
            string actual = string.Empty;
            try
            {
                PolicyHelper.GetDelegationPolicyPath(ResourceAttributeMatchType.ResourceRegistry, string.Empty, null, null, "50001337", 20001337, null, null, UuidType.NotSpecified);
            }
            catch (System.ArgumentException argEx)
            {
                actual = argEx.Message;
            }

            // Assert
            Assert.Equal(expectedArgumentException, actual);
        }

        private static XacmlMatch CreateSubjectMatch(string attributeId, string value)
        {
            return new XacmlMatch(
                new Uri(XacmlConstants.AttributeMatchFunction.StringEqual),
                new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), value),
                new XacmlAttributeDesignator(new Uri(XacmlConstants.MatchAttributeCategory.Subject), new Uri(attributeId), new Uri(XacmlConstants.DataTypes.XMLString), false));
        }
    }
}
