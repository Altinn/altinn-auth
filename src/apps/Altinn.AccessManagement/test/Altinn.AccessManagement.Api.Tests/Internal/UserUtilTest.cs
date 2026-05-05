using System.Security.Claims;
using Altinn.AccessManagement.Api.Internal.Utils;

// See: overhaul part-2 step 19
namespace Altinn.AccessManagement.Api.Tests.Internal;

/// <summary>
/// Pure-unit tests for <see cref="UserUtil.GetUserUuid"/>. Pins the
/// null-principal / null-claim / invalid-Guid defaults — a regression
/// would NRE on missing claims or default to <c>Guid.Empty</c> instead
/// of returning <see langword="null"/>.
/// </summary>
public class UserUtilTest
{
    private const string PartyUuidClaim = "urn:altinn:party:uuid";

    [Fact]
    public void GetUserUuid_NullPrincipal_ReturnsNull()
    {
        UserUtil.GetUserUuid(null!).Should().BeNull();
    }

    [Fact]
    public void GetUserUuid_NoClaims_ReturnsNull()
    {
        UserUtil.GetUserUuid(new ClaimsPrincipal(new ClaimsIdentity())).Should().BeNull();
    }

    [Fact]
    public void GetUserUuid_NoMatchingClaim_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("other", "value")], "test"));
        UserUtil.GetUserUuid(principal).Should().BeNull();
    }

    [Fact]
    public void GetUserUuid_InvalidGuid_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(PartyUuidClaim, "not-a-guid")], "test"));
        UserUtil.GetUserUuid(principal).Should().BeNull();
    }

    [Fact]
    public void GetUserUuid_ValidGuid_ReturnsParsedValue()
    {
        var expected = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(PartyUuidClaim, expected.ToString())], "test"));

        UserUtil.GetUserUuid(principal).Should().Be(expected);
    }
}
