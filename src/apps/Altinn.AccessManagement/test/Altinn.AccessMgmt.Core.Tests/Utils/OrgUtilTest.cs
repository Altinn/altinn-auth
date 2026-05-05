using System.Security.Claims;
using Altinn.AccessMgmt.Core.Utils;

namespace Altinn.AccessMgmt.Core.Tests.Utils;

public class OrgUtilTest
{
    // ── GetMaskinportenScopes ────────────────────────────────────────────────
    [Fact]
    public void GetMaskinportenScopes_ClaimPresent_ReturnsValue()
    {
        var principal = MakePrincipal(("scope", "altinn:accessmanagement/read"));
        OrgUtil.GetMaskinportenScopes(principal).Should().Be("altinn:accessmanagement/read");
    }

    [Fact]
    public void GetMaskinportenScopes_ClaimAbsent_ReturnsNull()
    {
        var principal = MakePrincipal();
        OrgUtil.GetMaskinportenScopes(principal).Should().BeNull();
    }

    [Fact]
    public void GetMaskinportenScopes_MultipleScopes_ReturnsRawValue()
    {
        var principal = MakePrincipal(("scope", "a b c"));
        OrgUtil.GetMaskinportenScopes(principal).Should().Be("a b c");
    }

    // ── GetAuthenticatedParty ────────────────────────────────────────────────
    [Fact]
    public void GetAuthenticatedParty_ValidConsumerClaim_ReturnsUrn()
    {
        var consumer = """{"ID":"0192:991825827","authority":"iso6523-actorid-upis"}""";
        var principal = MakePrincipal(("consumer", consumer));
        OrgUtil.GetAuthenticatedParty(principal).Should().NotBeNull();
    }

    [Fact]
    public void GetAuthenticatedParty_ConsumerClaimAbsent_ReturnsNull()
    {
        var principal = MakePrincipal();
        OrgUtil.GetAuthenticatedParty(principal).Should().BeNull();
    }

    [Fact]
    public void GetAuthenticatedParty_ConsumerClaimEmpty_ReturnsNull()
    {
        var principal = MakePrincipal(("consumer", string.Empty));
        OrgUtil.GetAuthenticatedParty(principal).Should().BeNull();
    }

    [Fact]
    public void GetAuthenticatedParty_WrongAuthority_ReturnsNull()
    {
        var consumer = """{"ID":"0192:991825827","authority":"some-other-authority"}""";
        var principal = MakePrincipal(("consumer", consumer));
        OrgUtil.GetAuthenticatedParty(principal).Should().BeNull();
    }

    [Fact]
    public void GetAuthenticatedParty_MissingIdField_ReturnsNull()
    {
        var consumer = """{"authority":"iso6523-actorid-upis"}""";
        var principal = MakePrincipal(("consumer", consumer));
        OrgUtil.GetAuthenticatedParty(principal).Should().BeNull();
    }

    [Fact]
    public void GetAuthenticatedParty_InvalidJson_ReturnsNull()
    {
        var principal = MakePrincipal(("consumer", "not-json"));
        OrgUtil.GetAuthenticatedParty(principal).Should().BeNull();
    }

    // ── GetSupplierParty ─────────────────────────────────────────────────────
    [Fact]
    public void GetSupplierParty_ValidSupplierClaim_ReturnsUrn()
    {
        var supplier = """{"ID":"0192:991825827","authority":"iso6523-actorid-upis"}""";
        var principal = MakePrincipal(("supplier", supplier));
        OrgUtil.GetSupplierParty(principal).Should().NotBeNull();
    }

    [Fact]
    public void GetSupplierParty_SupplierClaimAbsent_ReturnsNull()
    {
        var principal = MakePrincipal();
        OrgUtil.GetSupplierParty(principal).Should().BeNull();
    }

    [Fact]
    public void GetSupplierParty_WrongAuthority_ReturnsNull()
    {
        var supplier = """{"ID":"0192:991825827","authority":"wrong"}""";
        var principal = MakePrincipal(("supplier", supplier));
        OrgUtil.GetSupplierParty(principal).Should().BeNull();
    }

    [Fact]
    public void GetSupplierParty_IdWithNoColon_ReturnsNull()
    {
        var supplier = """{"ID":"991825827","authority":"iso6523-actorid-upis"}""";
        var principal = MakePrincipal(("supplier", supplier));
        OrgUtil.GetSupplierParty(principal).Should().BeNull();
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private static ClaimsPrincipal MakePrincipal(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.type, c.value)));
        return new ClaimsPrincipal(identity);
    }
}
