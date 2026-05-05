using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Authorization.Helper;
using Altinn.AccessManagement.Core.Constants;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Microsoft.AspNetCore.Http;

// See: overhaul part-2 step 22
namespace Altinn.AccessManagement.Enduser.Api.Tests.AuthorizationHelpers;

/// <summary>
/// Pure-unit tests for the Api.Enduser-specific
/// <see cref="DecisionHelper"/>'s accessors and PDP validation.
/// </summary>
public class DecisionHelperTest
{
    private static HttpContext CtxWith(params Claim[] claims)
        => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };

    private static HttpContext CtxWithQuery(string queryString)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString(queryString);
        return ctx;
    }

    // ── Claim accessors (mirror AuthenticationHelper but live in this helper) ─

    [Fact]
    public void GetUserPartyUuid_NoClaim_ReturnsEmpty()
        => DecisionHelper.GetUserPartyUuid(CtxWith()).Should().Be(Guid.Empty);

    [Fact]
    public void GetUserPartyUuid_ValidGuid_ReturnsParsedValue()
    {
        var guid = Guid.NewGuid();
        DecisionHelper.GetUserPartyUuid(CtxWith(new Claim(AltinnCoreClaimTypes.PartyUuid, guid.ToString())))
            .Should().Be(guid);
    }

    [Fact]
    public void GetUserPartyUuid_InvalidGuid_ReturnsEmpty()
        => DecisionHelper.GetUserPartyUuid(CtxWith(new Claim(AltinnCoreClaimTypes.PartyUuid, "not-a-guid"))).Should().Be(Guid.Empty);

    [Fact]
    public void GetUserId_NoClaim_ReturnsZero()
        => DecisionHelper.GetUserId(CtxWith()).Should().Be(0);

    [Fact]
    public void GetUserId_ValidClaim_ReturnsParsedValue()
        => DecisionHelper.GetUserId(CtxWith(new Claim(AltinnCoreClaimTypes.UserId, "42"))).Should().Be(42);

    [Fact]
    public void GetUserId_InvalidClaim_ReturnsZero()
        => DecisionHelper.GetUserId(CtxWith(new Claim(AltinnCoreClaimTypes.UserId, "abc"))).Should().Be(0);

    // ── Querystring accessors ────────────────────────────────────────────────

    [Fact]
    public void GetFromParam_ValidGuid_ReturnsParsed()
    {
        var guid = Guid.NewGuid();
        DecisionHelper.GetFromParam(CtxWithQuery($"?from={guid}")).Should().Be(guid);
    }

    [Fact]
    public void GetFromParam_MissingOrInvalid_ReturnsNull()
    {
        DecisionHelper.GetFromParam(CtxWithQuery(string.Empty)).Should().BeNull();
        DecisionHelper.GetFromParam(CtxWithQuery("?from=not-a-guid")).Should().BeNull();
    }

    [Fact]
    public void GetToParam_ValidGuid_ReturnsParsed()
    {
        var guid = Guid.NewGuid();
        DecisionHelper.GetToParam(CtxWithQuery($"?to={guid}")).Should().Be(guid);
    }

    [Fact]
    public void GetToParam_MissingOrInvalid_ReturnsNull()
    {
        DecisionHelper.GetToParam(CtxWithQuery(string.Empty)).Should().BeNull();
        DecisionHelper.GetToParam(CtxWithQuery("?to=not-a-guid")).Should().BeNull();
    }

    [Fact]
    public void GetPartyParam_ValidGuid_ReturnsParsed()
    {
        var guid = Guid.NewGuid();
        DecisionHelper.GetPartyParam(CtxWithQuery($"?party={guid}")).Should().Be(guid);
    }

    [Fact]
    public void GetPartyParam_MissingOrInvalid_ReturnsNull()
    {
        DecisionHelper.GetPartyParam(CtxWithQuery(string.Empty)).Should().BeNull();
        DecisionHelper.GetPartyParam(CtxWithQuery("?party=not-a-guid")).Should().BeNull();
    }

    // ── ValidatePdpDecision ──────────────────────────────────────────────────

    [Fact]
    public void ValidatePdpDecision_NullResponse_Throws()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.Throws<ArgumentNullException>(() => DecisionHelper.ValidatePdpDecision(null!, user));
    }

    [Fact]
    public void ValidatePdpDecision_NullUser_Throws()
    {
        var response = new XacmlJsonResponse { Response = [new() { Decision = "Permit" }] };
        Assert.Throws<ArgumentNullException>(() => DecisionHelper.ValidatePdpDecision(response, null!));
    }

    [Fact]
    public void ValidatePdpDecision_ZeroResults_ReturnsFalse()
    {
        var response = new XacmlJsonResponse { Response = [] };
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        DecisionHelper.ValidatePdpDecision(response, user).Should().BeFalse();
    }

    [Fact]
    public void ValidatePdpDecision_MultipleResults_ReturnsFalse()
    {
        var response = new XacmlJsonResponse
        {
            Response = [new() { Decision = "Permit" }, new() { Decision = "Permit" }],
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        DecisionHelper.ValidatePdpDecision(response, user).Should().BeFalse();
    }

    [Fact]
    public void ValidatePdpDecision_PermitWithNoObligations_ReturnsTrue()
    {
        var response = new XacmlJsonResponse { Response = [new() { Decision = "Permit" }] };
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        DecisionHelper.ValidatePdpDecision(response, user).Should().BeTrue();
    }

    [Fact]
    public void ValidatePdpDecision_NonPermit_ReturnsFalse()
    {
        var response = new XacmlJsonResponse { Response = [new() { Decision = "Deny" }] };
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        DecisionHelper.ValidatePdpDecision(response, user).Should().BeFalse();
    }

    [Fact]
    public void ValidatePdpDecision_PermitWithMinAuthLevel_UserBelow_ReturnsFalse()
    {
        var response = new XacmlJsonResponse
        {
            Response =
            [
                new()
                {
                    Decision = "Permit",
                    Obligations =
                    [
                        new()
                        {
                            AttributeAssignment =
                            [
                                new()
                                {
                                    Category = "urn:altinn:minimum-authenticationlevel",
                                    Value = "3",
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("urn:altinn:authlevel", "2")], "test"));

        DecisionHelper.ValidatePdpDecision(response, user).Should().BeFalse();
    }

    [Fact]
    public void ValidatePdpDecision_PermitWithMinAuthLevel_UserMeetsLevel_ReturnsTrue()
    {
        var response = new XacmlJsonResponse
        {
            Response =
            [
                new()
                {
                    Decision = "Permit",
                    Obligations =
                    [
                        new()
                        {
                            AttributeAssignment =
                            [
                                new()
                                {
                                    Category = "urn:altinn:minimum-authenticationlevel",
                                    Value = "3",
                                },
                            ],
                        },
                    ],
                },
            ],
        };

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("urn:altinn:authlevel", "4")], "test"));

        DecisionHelper.ValidatePdpDecision(response, user).Should().BeTrue();
    }
}
