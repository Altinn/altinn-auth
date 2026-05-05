using System.Security.Claims;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Microsoft.AspNetCore.Http;

// See: overhaul part-2 step 17
namespace Altinn.AccessManagement.Tests.Helpers;

/// <summary>
/// Pure-unit tests for <see cref="AuthenticationHelper"/>. Pins the
/// claim-extraction defaults (missing claim → 0 / Guid.Empty / "" rather
/// than NRE), the int/Guid TryParse fallback, the JSON-deserialization
/// path for system-user uuids, and the GetAuthenticatedPartyUuid
/// composition (PartyUuid wins over SystemUserUuid when both present).
/// </summary>
public class AuthenticationHelperTest
{
    private static HttpContext CtxWith(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private static HttpContext CtxWithNoUser() => new DefaultHttpContext { User = null };

    // ── GetUserId ─────────────────────────────────────────────────────────────
    [Fact]
    public void GetUserId_NoClaim_ReturnsZero()
    {
        AuthenticationHelper.GetUserId(CtxWith()).Should().Be(0);
    }

    [Fact]
    public void GetUserId_ValidClaim_ReturnsParsedValue()
    {
        AuthenticationHelper.GetUserId(CtxWith(new Claim(AltinnCoreClaimTypes.UserId, "42"))).Should().Be(42);
    }

    [Fact]
    public void GetUserId_NonNumericClaim_ReturnsZero()
    {
        // int.TryParse fallback — non-numeric value returns 0, doesn't throw.
        AuthenticationHelper.GetUserId(CtxWith(new Claim(AltinnCoreClaimTypes.UserId, "not-a-number"))).Should().Be(0);
    }

    [Fact]
    public void GetUserId_NullUser_ReturnsZero()
    {
        // Null-conditional on context.User?.Claims keeps this from NRE-ing.
        AuthenticationHelper.GetUserId(CtxWithNoUser()).Should().Be(0);
    }

    // ── GetPartyUuid ──────────────────────────────────────────────────────────
    [Fact]
    public void GetPartyUuid_NoClaim_ReturnsEmpty()
    {
        AuthenticationHelper.GetPartyUuid(CtxWith()).Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetPartyUuid_ValidGuid_ReturnsParsedValue()
    {
        var guid = Guid.NewGuid();
        AuthenticationHelper.GetPartyUuid(CtxWith(new Claim(AltinnCoreClaimTypes.PartyUuid, guid.ToString()))).Should().Be(guid);
    }

    [Fact]
    public void GetPartyUuid_InvalidGuid_ReturnsEmpty()
    {
        AuthenticationHelper.GetPartyUuid(CtxWith(new Claim(AltinnCoreClaimTypes.PartyUuid, "not-a-guid"))).Should().Be(Guid.Empty);
    }

    // ── GetPartyId ────────────────────────────────────────────────────────────
    [Fact]
    public void GetPartyId_NoClaim_ReturnsZero()
    {
        AuthenticationHelper.GetPartyId(CtxWith()).Should().Be(0);
    }

    [Fact]
    public void GetPartyId_ValidClaim_ReturnsParsedValue()
    {
        AuthenticationHelper.GetPartyId(CtxWith(new Claim(AltinnCoreClaimTypes.PartyID, "12345"))).Should().Be(12345);
    }

    // ── GetUserAuthenticationLevel ────────────────────────────────────────────
    [Fact]
    public void GetUserAuthenticationLevel_NoClaim_ReturnsZero()
    {
        AuthenticationHelper.GetUserAuthenticationLevel(CtxWith()).Should().Be(0);
    }

    [Fact]
    public void GetUserAuthenticationLevel_ValidClaim_ReturnsParsedValue()
    {
        AuthenticationHelper.GetUserAuthenticationLevel(CtxWith(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "3"))).Should().Be(3);
    }

    // ── GetSystemUserUuid / GetSystemUserUuidString ──────────────────────────
    [Fact]
    public void GetSystemUserUuid_NoAuthorizationDetailsClaim_ReturnsEmpty()
    {
        AuthenticationHelper.GetSystemUserUuid(CtxWith()).Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetSystemUserUuid_AuthorizationDetailsWithSystemUserType_ReturnsFirstSystemUserId()
    {
        var sysuserId = Guid.NewGuid();
        var json = $$"""{"type":"urn:altinn:systemuser","systemuser_id":["{{sysuserId}}"]}""";

        AuthenticationHelper.GetSystemUserUuid(CtxWith(new Claim("authorization_details", json))).Should().Be(sysuserId);
    }

    [Fact]
    public void GetSystemUserUuid_AuthorizationDetailsWithOtherType_ReturnsEmpty()
    {
        var json = """{"type":"urn:other","systemuser_id":["00000000-0000-0000-0000-000000000001"]}""";

        AuthenticationHelper.GetSystemUserUuid(CtxWith(new Claim("authorization_details", json))).Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetSystemUserUuidString_AuthorizationDetailsWithSystemUserType_ReturnsFirstSystemUserId()
    {
        var json = """{"type":"urn:altinn:systemuser","systemuser_id":["abc-123"]}""";

        AuthenticationHelper.GetSystemUserUuidString(CtxWith(new Claim("authorization_details", json))).Should().Be("abc-123");
    }

    [Fact]
    public void GetSystemUserUuidString_NoClaim_ReturnsEmptyString()
    {
        AuthenticationHelper.GetSystemUserUuidString(CtxWith()).Should().BeEmpty();
    }

    // ── GetAuthenticatedPartyUuid composition ────────────────────────────────
    [Fact]
    public void GetAuthenticatedPartyUuid_PartyUuidPresent_ReturnsPartyUuid()
    {
        var party = Guid.NewGuid();
        var sysuser = Guid.NewGuid();
        var json = $$"""{"type":"urn:altinn:systemuser","systemuser_id":["{{sysuser}}"]}""";

        var ctx = CtxWith(
            new Claim(AltinnCoreClaimTypes.PartyUuid, party.ToString()),
            new Claim("authorization_details", json));

        AuthenticationHelper.GetAuthenticatedPartyUuid(ctx).Should().Be(party);
    }

    [Fact]
    public void GetAuthenticatedPartyUuid_NoPartyUuid_FallsBackToSystemUserUuid()
    {
        var sysuser = Guid.NewGuid();
        var json = $$"""{"type":"urn:altinn:systemuser","systemuser_id":["{{sysuser}}"]}""";

        AuthenticationHelper.GetAuthenticatedPartyUuid(CtxWith(new Claim("authorization_details", json))).Should().Be(sysuser);
    }

    [Fact]
    public void GetAuthenticatedPartyUuid_NeitherClaim_ReturnsEmpty()
    {
        AuthenticationHelper.GetAuthenticatedPartyUuid(CtxWith()).Should().Be(Guid.Empty);
    }
}
