using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Enums;

namespace Altinn.AccessManagement.Tests.Unit.Helpers
{
    /// <summary>
    /// Test class for <see cref="PolicyHelper"></see>
    /// </summary>
    [UnitTest]
    public class PolicyHelperTest
    {
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
    }
}
