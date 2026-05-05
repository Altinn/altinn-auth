using System.Security.Claims;
using Altinn.Platform.Authenticaiton.Extensions;
using AltinnCore.Authentication.Constants;

namespace Altinn.Platform.Authorization.Tests.ExtensionTests;

public class ClaimsPrincipalExtensionsTest
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    // --- GetUserOrOrgId ---
    [Fact]
    public void GetUserOrOrgId_HasUserId_ReturnsUserId()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.UserId, "12345"));
        Assert.Equal("12345", user.GetUserOrOrgId());
    }

    [Fact]
    public void GetUserOrOrgId_HasOrgNumber_ReturnsOrgNumber()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.OrgNumber, "987654321"));
        Assert.Equal("987654321", user.GetUserOrOrgId());
    }

    [Fact]
    public void GetUserOrOrgId_NoClaims_ReturnsNull()
    {
        var user = CreatePrincipal();
        Assert.Null(user.GetUserOrOrgId());
    }

    [Fact]
    public void GetUserOrOrgId_HasBoth_PrefersUserId()
    {
        var user = CreatePrincipal(
            new Claim(AltinnCoreClaimTypes.UserId, "111"),
            new Claim(AltinnCoreClaimTypes.OrgNumber, "222"));
        Assert.Equal("111", user.GetUserOrOrgId());
    }

    // --- GetOrg ---
    [Fact]
    public void GetOrg_HasClaim_ReturnsValue()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.Org, "ttd"));
        Assert.Equal("ttd", user.GetOrg());
    }

    [Fact]
    public void GetOrg_NoClaim_ReturnsNull()
    {
        var user = CreatePrincipal();
        Assert.Null(user.GetOrg());
    }

    // --- GetOrgNumber ---
    [Fact]
    public void GetOrgNumber_ValidInt_ReturnsValue()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.OrgNumber, "123456789"));
        Assert.Equal(123456789, user.GetOrgNumber());
    }

    [Fact]
    public void GetOrgNumber_NonNumeric_ReturnsNull()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.OrgNumber, "abc"));
        Assert.Null(user.GetOrgNumber());
    }

    [Fact]
    public void GetOrgNumber_NoClaim_ReturnsNull()
    {
        var user = CreatePrincipal();
        Assert.Null(user.GetOrgNumber());
    }

    // --- GetUserIdAsInt ---
    [Fact]
    public void GetUserIdAsInt_ValidInt_ReturnsValue()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.UserId, "42"));
        Assert.Equal(42, user.GetUserIdAsInt());
    }

    [Fact]
    public void GetUserIdAsInt_NonNumeric_ReturnsNull()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.UserId, "notanumber"));
        Assert.Null(user.GetUserIdAsInt());
    }

    [Fact]
    public void GetUserIdAsInt_NoClaim_ReturnsNull()
    {
        var user = CreatePrincipal();
        Assert.Null(user.GetUserIdAsInt());
    }

    // --- GetAuthenticationLevel ---
    [Fact]
    public void GetAuthenticationLevel_ValidLevel_ReturnsValue()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "3"));
        Assert.Equal(3, user.GetAuthenticationLevel());
    }

    [Fact]
    public void GetAuthenticationLevel_NoClaim_ReturnsZero()
    {
        var user = CreatePrincipal();
        Assert.Equal(0, user.GetAuthenticationLevel());
    }

    [Fact]
    public void GetAuthenticationLevel_NonNumeric_ReturnsZero()
    {
        var user = CreatePrincipal(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "high"));
        Assert.Equal(0, user.GetAuthenticationLevel());
    }
}
