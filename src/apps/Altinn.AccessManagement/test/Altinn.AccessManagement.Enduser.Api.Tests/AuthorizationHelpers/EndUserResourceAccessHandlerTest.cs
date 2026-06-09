using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Authorization.AuthorizationHandler;
using Altinn.AccessManagement.Api.Enduser.Authorization.AuthorizationRequirement;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Altinn.AccessManagement.Enduser.Api.Tests.AuthorizationHelpers;

/// <summary>
/// Tests for <see cref="EndUserResourceAccessHandler"/>, focused on the handling of a
/// missing or malformed <c>party</c> query parameter (which previously surfaced as a 500).
/// </summary>
[UnitTest]
public class EndUserResourceAccessHandlerTest
{
    private static (EndUserResourceAccessHandler Handler, Mock<IPDP> Pdp, HttpContext HttpContext) CreateSut(string queryString)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString(queryString);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var pdp = new Mock<IPDP>();

        var handler = new EndUserResourceAccessHandler(
            accessor.Object,
            pdp.Object,
            NullLogger<EndUserResourceAccessHandler>.Instance);

        return (handler, pdp, httpContext);
    }

    private static AuthorizationHandlerContext Ctx(EndUserResourceAccessRequirement requirement)
        => new([requirement], new ClaimsPrincipal(new ClaimsIdentity("test")), null);

    [Fact]
    public async Task HandleRequirement_InvalidPartyParam_Fails_WithoutCallingPdp()
    {
        var (handler, pdp, _) = CreateSut("?party=not-a-guid");
        var context = Ctx(new EndUserResourceAccessRequirement("read", "altinn_maskinporten_scope_delegation", false));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
        context.HasSucceeded.Should().BeFalse();
        pdp.Verify(p => p.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequirement_MissingPartyParam_Fails()
    {
        var (handler, _, _) = CreateSut(string.Empty);
        var context = Ctx(new EndUserResourceAccessRequirement("read", "altinn_maskinporten_scope_delegation", false));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_InvalidPartyParam_WithAllowUnauthorizedParty_StillFails()
    {
        // AllowAllowUnauthorizedParty only governs whether the user must be authorized for an
        // otherwise valid party. It does not excuse a missing or malformed party.
        var (handler, pdp, _) = CreateSut("?party=not-a-guid");
        var context = Ctx(new EndUserResourceAccessRequirement("read", "res", allowAllowUnauthorizedParty: true));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
        context.HasSucceeded.Should().BeFalse();
        pdp.Verify(p => p.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequirement_EmptyPartyParam_Fails()
    {
        var (handler, pdp, _) = CreateSut("?party=");
        var context = Ctx(new EndUserResourceAccessRequirement("read", "altinn_maskinporten_scope_delegation", false));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
        pdp.Verify(p => p.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequirement_WhitespacePartyParam_Fails()
    {
        var (handler, _, _) = CreateSut("?party=%20");
        var context = Ctx(new EndUserResourceAccessRequirement("read", "altinn_maskinporten_scope_delegation", false));

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ValidParty_PdpPermit_Succeeds()
    {
        var (handler, pdp, httpContext) = CreateSut($"?party={Guid.NewGuid()}");
        pdp.Setup(p => p.GetDecisionForRequest(It.IsAny<XacmlJsonRequestRoot>()))
           .ReturnsAsync(new XacmlJsonResponse { Response = [new() { Decision = "Permit" }] });
        var context = Ctx(new EndUserResourceAccessRequirement("read", "res", false));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        httpContext.Items["HasRequestedPermission"].Should().Be(true);
    }
}
