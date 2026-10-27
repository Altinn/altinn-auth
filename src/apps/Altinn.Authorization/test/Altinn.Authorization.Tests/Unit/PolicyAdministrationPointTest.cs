using Altinn.Authorization.Tests.MockServices;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.Authorization.Tests.Unit
{
    [Collection(PolicyAdministrationPointCollection.Name)]
    [UnitTest]
    public class PolicyAdministrationPointTest
    {
        private readonly IPolicyAdministrationPoint _pap = new PolicyAdministrationPoint(
            new PolicyRepositoryMock(new Mock<ILogger<PolicyRepositoryMock>>().Object));

        /// <summary>
        /// Test case: Write to storage a file.
        /// Expected: WritePolicyAsync returns true.
        /// </summary>
        [Fact]
        public async Task WritePolicyAsync_ValidPolicyStream_ReturnsTrue()
        {
            // Arrange
            Stream dataStream = File.OpenRead("Data/Policies/policy.xml");

            // Act
            bool successfullyStored = await _pap.WritePolicyAsync("org", "app", dataStream);
            TestSetupUtil.DeleteAppBlobData("org", "app");

            // Assert
            Assert.True(successfullyStored);
        }

        /// <summary>
        /// Test case: Write a file to storage where the org parameter arguments is empty.
        /// Expected: WritePolicyAsync throws ArgumentException.
        /// </summary>
        [Fact]
        public async Task WritePolicyAsync_EmptyOrg_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _pap.WritePolicyAsync(string.Empty, "app", new MemoryStream()));
        }

        /// <summary>
        /// Test case: Write a file to storage where the app parameter arguments is empty.
        /// Expected: WritePolicyAsync throws ArgumentException.
        /// </summary>
        [Fact]
        public async Task WritePolicyAsync_EmptyApp_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _pap.WritePolicyAsync("org", string.Empty, new MemoryStream()));
        }

        /// <summary>
        /// Test case: Write to storage a file that is null.
        /// Expected: WritePolicyAsync throws ArgumentException.
        /// </summary>
        [Fact]
        public async Task WritePolicyAsync_NullStream_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _pap.WritePolicyAsync("org", "app", null));
        }
    }
}
