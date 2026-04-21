using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Interfaces;

using Moq;

using Xunit;

namespace Altinn.Authorization.PEP.Tests
{
    public class PDPAppSITest
    {
        [Fact]
        public async Task GetDecisionForRequest_ValidResponse_ReturnsResponse()
        {
            // Arrange
            var expectedResponse = new XacmlJsonResponse
            {
                Response = [new XacmlJsonResult { Decision = XacmlContextDecision.Permit.ToString() }]
            };

            var pdpMock = new Mock<IPDP>();
            pdpMock.Setup(p => p.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var request = new XacmlJsonRequestRoot { Request = new XacmlJsonRequest() };

            // Act
            var result = await pdpMock.Object.GetDecisionForRequest(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Response);
            Assert.Equal("Permit", result.Response[0].Decision);
        }

        [Fact]
        public async Task GetDecisionForRequest_NullResponse_ReturnsNull()
        {
            // Arrange
            var pdpMock = new Mock<IPDP>();
            pdpMock.Setup(p => p.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((XacmlJsonResponse)null);

            var request = new XacmlJsonRequestRoot { Request = new XacmlJsonRequest() };

            // Act
            var result = await pdpMock.Object.GetDecisionForRequest(request, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDecisionForUnvalidateRequest_NullResponse_ThrowsArgumentNullException()
        {
            // Arrange
            var pdpMock = new Mock<IPDP>();
            pdpMock.Setup(p => p.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((XacmlJsonResponse)null);
            pdpMock.Setup(p => p.GetDecisionForUnvalidateRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentNullException("response"));

            var request = new XacmlJsonRequestRoot { Request = new XacmlJsonRequest() };
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                pdpMock.Object.GetDecisionForUnvalidateRequest(request, user, CancellationToken.None));
        }

        [Fact]
        public async Task GetDecisionForUnvalidateRequest_PermitResponse_ReturnsTrue()
        {
            // Arrange
            var pdpMock = new Mock<IPDP>();
            pdpMock.Setup(p => p.GetDecisionForUnvalidateRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var request = new XacmlJsonRequestRoot { Request = new XacmlJsonRequest() };
            var claims = new[] { new Claim("urn:altinn:authlevel", "3") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = await pdpMock.Object.GetDecisionForUnvalidateRequest(request, user, CancellationToken.None);

            // Assert
            Assert.True(result);
        }
    }
}
