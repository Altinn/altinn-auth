using System.Security.Claims;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Utilities;

// See: overhaul part-2 step 18
namespace Altinn.AccessManagement.Tests.Utilities;

/// <summary>
/// Pure-unit tests for <see cref="MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess"/>.
/// Pins the authorization decision matrix:
///
///   1. delegations.admin scope present → always authorized.
///   2. otherwise the requested scope must start with one of the
///      consumer's prefixes (followed by a colon).
///
/// This is the gate for delegation-lookup endpoints; a false-positive
/// would let an unauthorized consumer enumerate other parties'
/// delegations, and a false-negative would block legitimate
/// supplier/admin lookups.
/// </summary>
public class MaskinportenSchemaAuthorizerTest
{
    private const string AdminScope = AuthzConstants.SCOPE_MASKINPORTEN_DELEGATIONS_ADMIN;
    private const string ScopeClaim = AuthzConstants.CLAIM_MASKINPORTEN_SCOPE;
    private const string PrefixClaim = AuthzConstants.CLAIM_MASKINPORTEN_CONSUMER_PREFIX;

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    // ── Admin scope short-circuit ─────────────────────────────────────────────

    [Fact]
    public void HasAdminScope_AuthorizesAnyScope()
    {
        var principal = Principal(new Claim(ScopeClaim, AdminScope));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:any/scope", principal).Should().BeTrue();
    }

    [Fact]
    public void HasAdminScope_AuthorizesEvenWithEmptyRequestedScope()
    {
        // Admin scope wins before the IsNullOrWhiteSpace guard fires.
        var principal = Principal(new Claim(ScopeClaim, AdminScope));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess(string.Empty, principal).Should().BeTrue();
    }

    [Fact]
    public void HasAdminScope_AmongOtherScopes_StillAuthorizes()
    {
        var principal = Principal(new Claim(ScopeClaim, $"some:other {AdminScope} another:scope"));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("any:scope", principal).Should().BeTrue();
    }

    // ── Empty / whitespace requested scope without admin → unauthorized ───────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoAdminScope_AndEmptyOrWhitespaceRequested_Unauthorized(string? requested)
    {
        var principal = Principal(new Claim(PrefixClaim, "some-prefix"));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess(requested, principal).Should().BeFalse();
    }

    // ── Prefix matching ───────────────────────────────────────────────────────

    [Fact]
    public void RequestedScopeStartsWithConsumerPrefixColon_Authorizes()
    {
        var principal = Principal(new Claim(PrefixClaim, "altinn:somesupplier"));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:somesupplier:read", principal).Should().BeTrue();
    }

    [Fact]
    public void RequestedScope_NoMatchingPrefix_Unauthorized()
    {
        var principal = Principal(new Claim(PrefixClaim, "altinn:somesupplier"));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:other:read", principal).Should().BeFalse();
    }

    [Fact]
    public void RequestedScopeMatchesPrefix_ButMissingColon_Unauthorized()
    {
        // "altinn:supplier" is the prefix; without ':' separator a
        // scope like "altinn:supplierfoo" must NOT match — pinning the
        // colon-boundary check.
        var principal = Principal(new Claim(PrefixClaim, "altinn:supplier"));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:supplierfoo:read", principal).Should().BeFalse();
    }

    [Fact]
    public void MultipleConsumerPrefixes_AnyMatch_Authorizes()
    {
        var principal = Principal(
            new Claim(PrefixClaim, "altinn:abc"),
            new Claim(PrefixClaim, "altinn:xyz"));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:xyz:read", principal).Should().BeTrue();
    }

    [Fact]
    public void NoConsumerPrefixes_AndNoAdminScope_Unauthorized()
    {
        var principal = Principal(); // no claims at all

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:anything:read", principal).Should().BeFalse();
    }

    [Fact]
    public void ScopeClaimWithoutAdmin_StillFallsThroughToPrefixCheck()
    {
        // Having a non-admin scope claim alone doesn't authorize; the prefix
        // claim still has to match.
        var principal = Principal(
            new Claim(ScopeClaim, "altinn:somescope"),
            new Claim(PrefixClaim, "altinn:supplier"));

        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:supplier:read", principal).Should().BeTrue();
        MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess("altinn:other:read", principal).Should().BeFalse();
    }
}
