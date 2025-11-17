using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Threading.Tasks;

using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Configuration;
using Altinn.Common.PEP.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.Common.PEP.Authorization
{
    public class ResourceAccessHandlerTest
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IPDP> _pdpMock;
        private readonly IOptions<PepSettings> _generalSettings;
        private readonly ResourceAccessHandler _rah;

        public ResourceAccessHandlerTest()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _pdpMock = new Mock<IPDP>();
            _generalSettings = Options.Create(new PepSettings());
            _rah = new ResourceAccessHandler(_httpContextAccessorMock.Object, _pdpMock.Object, new Mock<ILogger<ResourceAccessHandler>>().Object);
        }

        /// <summary>
        /// Test case: Send request and get response that fulfills all requirements
        /// Expected: Context will succeed
        /// </summary>
        [Fact]
        public async Task HandleRequirementAsync_TC01Async()
        {
            // Arrange 
            AuthorizationHandlerContext context = CreateAuthorizationHandlerContext();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(CreateHttpContext("23453546", null, null));
            XacmlJsonResponse response = CreateResponse(XacmlContextDecision.Permit.ToString());
            _pdpMock.Setup(a => a.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            // Act
            await _rah.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);
        }

        /// <summary>
        /// Test case: Send request and get response that fulfills all requirements
        /// Expected: Context will succeed
        /// </summary>
        [Fact]
        public async Task HandleRequirementAsync_TC02Async()
        {
            // Arrange 
            AuthorizationHandlerContext context = CreateAuthorizationHandlerContext();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(CreateHttpContext("organization", "991825827", null));
            XacmlJsonResponse response = CreateResponse(XacmlContextDecision.Permit.ToString());
            _pdpMock.Setup(a => a.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            // Act
            await _rah.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);

            XacmlJsonRequestRoot request = _pdpMock.Invocations[0].Arguments[0] as XacmlJsonRequestRoot;
            Assert.Equal("urn:altinn:organizationnumber", request.Request.Resource[0].Attribute[0].AttributeId);
            Assert.Equal("991825827", request.Request.Resource[0].Attribute[0].Value);
        }

        /// <summary>
        /// Test case: Send request and get response that fulfills all requirements
        /// Expected: Exception since format of who is incorrect
        /// </summary>
        [Fact]

        public async Task HandleRequirementAsync_TC03Async()
        {
            // Arrange 
            AuthorizationHandlerContext context = CreateAuthorizationHandlerContext();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(CreateHttpContext("organization", "991825827M", null));
            XacmlJsonResponse response = CreateResponse(XacmlContextDecision.Permit.ToString());
            _pdpMock.Setup(a => a.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            // Act
            Task Act() => _rah.HandleAsync(context);

            // Assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.Equal("invalid party organization", exception.Message);
        }

        /// <summary>
        /// Test case: Send request and get response that fulfills all requirements
        /// Expected: True
        /// </summary>
        [Fact]

        public async Task HandleRequirementAsync_TC04Async()
        {
            // Arrange 
            AuthorizationHandlerContext context = CreateAuthorizationHandlerContext();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(CreateHttpContext("person", null, "01014922047"));
            XacmlJsonResponse response = CreateResponse(XacmlContextDecision.Permit.ToString());
            _pdpMock.Setup(a => a.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            // Act
            await _rah.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.False(context.HasFailed);

            XacmlJsonRequestRoot request = _pdpMock.Invocations[0].Arguments[0] as XacmlJsonRequestRoot;
            Assert.Equal("urn:altinn:person:identifier-no", request.Request.Resource[0].Attribute[0].AttributeId);
            Assert.Equal("01014922047", request.Request.Resource[0].Attribute[0].Value);
        }

        /// <summary>
        /// Test case: Send request and get response that fulfills all requirements
        /// Expected: Exception since format of who is incorrect
        /// </summary>
        [Fact]

        public async Task HandleRequirementAsync_TC05Async()
        {
            // Arrange 
            AuthorizationHandlerContext context = CreateAuthorizationHandlerContext();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(CreateHttpContext("person", null, "a01014922047"));
            XacmlJsonResponse response = CreateResponse(XacmlContextDecision.Permit.ToString());
            _pdpMock.Setup(a => a.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            // Act
            Task Act() => _rah.HandleAsync(context);

            // Assert
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.Equal("invalid party person", exception.Message);
        }

        /// <summary>
        /// Test case: Send request and verify the XForwardedForHeader property in request
        /// Expected: Request header does not have a xforwardedforheader and therefore the header property in xacmljsonrequest will be null
        /// </summary>
        [Fact]
        public async Task HandleRequirementAsync_TC06Async()
        {
            // Arrange 
            AuthorizationHandlerContext context = CreateAuthorizationHandlerContext();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(CreateHttpContext("23453546", null, null));
            XacmlJsonResponse response = CreateResponse(XacmlContextDecision.Permit.ToString());

            // Verify
            _pdpMock.Setup(a => a.GetDecisionForRequest(It.Is<XacmlJsonRequestRoot>(xr => xr.Request.XForwardedForHeader == null), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            // Act
            await _rah.HandleAsync(context);
        }

        /// <summary>
        /// Test case: Send request verify if the ipaddress from the x-forwarded-for header is received
        /// Expected: XForwardedForHeader proeprty in request receives the ipaddress from the header
        /// </summary>
        [Fact]
        public async Task HandleRequirementAsync_TC07Async()
        {
            // Arrange 
            AuthorizationHandlerContext context = CreateAuthorizationHandlerContext();
            string ipaddress = "18.203.138.153";
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(CreateHttpContext("organization", "991825827", null, ipaddress));
            XacmlJsonResponse response = CreateResponse(XacmlContextDecision.Permit.ToString());

            // verify
            _pdpMock.Setup(a => a.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

            // Act
            await _rah.HandleAsync(context);
        }

        private ClaimsPrincipal CreateUser()
        {
            // Create the user
            List<Claim> claims = new List<Claim>();

            // type, value, valuetype, issuer
            claims.Add(new Claim("urn:name", "Ola", "string", "org"));
            claims.Add(new Claim("urn:altinn:authlevel", "2", "string", "org"));

            ClaimsPrincipal user = new ClaimsPrincipal(new ClaimsIdentity(claims));

            return user;
        }

        private HttpContext CreateHttpContext(string party, string orgHeader, string ssnHeader, string xForwardedForHeader = null)
        {
            HttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues.Add("party", party);
            if (!string.IsNullOrEmpty(orgHeader))
            {
                httpContext.Request.Headers.Add("Altinn-Party-OrganizationNumber", orgHeader);
            }

            if (!string.IsNullOrEmpty(ssnHeader))
            {
                httpContext.Request.Headers.Add("Altinn-Party-SocialSecurityNumber", ssnHeader);
            }

            if (!string.IsNullOrEmpty(xForwardedForHeader))
            {
                httpContext.Request.Headers.Add("x-forwarded-for", xForwardedForHeader);
            }

            return httpContext;
        }

        private XacmlJsonResponse CreateResponse(string decision)
        {
            // Create response 
            XacmlJsonResponse response = new XacmlJsonResponse();
            response.Response = new List<XacmlJsonResult>();

            // Set result to premit
            XacmlJsonResult result = new XacmlJsonResult();
            result.Decision = decision;
            response.Response.Add(result);

            return response;
        }

        private XacmlJsonResponse AddObligationWithMinAuthLv(XacmlJsonResponse response, string minAuthLv)
        {
            // Add obligation to result with a minimum authentication level attribute
            XacmlJsonResult result = response.Response[0];
            XacmlJsonObligationOrAdvice obligation = new XacmlJsonObligationOrAdvice();
            obligation.AttributeAssignment = new List<XacmlJsonAttributeAssignment>();
            XacmlJsonAttributeAssignment authenticationAttribute = new XacmlJsonAttributeAssignment()
            {
                Category = "urn:altinn:minimum-authenticationlevel",
                Value = minAuthLv
            };
            obligation.AttributeAssignment.Add(authenticationAttribute);
            result.Obligations = new List<XacmlJsonObligationOrAdvice>();
            result.Obligations.Add(obligation);

            return response;
        }

        private AuthorizationHandlerContext CreateAuthorizationHandlerContext()
        {
            ResourceAccessRequirement requirement = new ResourceAccessRequirement("read", "altinn_access_management_apidelegation");
            ClaimsPrincipal user = CreateUser();
            Document resource = default(Document);
            AuthorizationHandlerContext context = new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource);
            return context;
        }
    }
}
