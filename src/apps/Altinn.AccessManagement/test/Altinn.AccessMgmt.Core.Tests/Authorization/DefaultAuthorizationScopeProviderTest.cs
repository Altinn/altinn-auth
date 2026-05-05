using Altinn.AccessMgmt.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// See: overhaul part-2 step 13
namespace Altinn.AccessMgmt.Core.Tests.Authorization;

/// <summary>
/// Pure-unit tests for the internal <see cref="DefaultAuthorizationScopeProvider"/>.
/// Pins the two scope-claim sources the provider yields from: federation-identity
/// `urn:altinn:scope` claims (one scope per claim) and top-level "scope" claims
/// (space-separated values, split by the provider).
/// </summary>
public class DefaultAuthorizationScopeProviderTest
{
    private static AuthorizationHandlerContext MakeContext(ClaimsPrincipal user) =>
        new([], user, null);

    private static ClaimsIdentity FederationIdentity(params Claim[] claims) =>
        new(claims, "AuthenticationTypes.Federation");

    private static ClaimsIdentity OtherIdentity(params Claim[] claims) =>
        new(claims, "OtherAuth");

    [Fact]
    public void FederationIdentity_AltinnScopeClaim_Yielded()
    {
        var identity = FederationIdentity(new Claim("urn:altinn:scope", "altinn:portal"));
        var ctx = MakeContext(new ClaimsPrincipal(identity));

        var scopes = new DefaultAuthorizationScopeProvider().GetScopeStrings(ctx).ToList();

        Assert.Equal(["altinn:portal"], scopes);
    }

    [Fact]
    public void FederationIdentity_NonScopeClaim_NotYielded()
    {
        var identity = FederationIdentity(new Claim("name", "Alice"));
        var ctx = MakeContext(new ClaimsPrincipal(identity));

        var scopes = new DefaultAuthorizationScopeProvider().GetScopeStrings(ctx).ToList();

        Assert.Empty(scopes);
    }

    [Fact]
    public void NonFederationIdentity_AltinnScopeClaim_NotYielded()
    {
        var identity = OtherIdentity(new Claim("urn:altinn:scope", "altinn:portal"));
        var ctx = MakeContext(new ClaimsPrincipal(identity));

        var scopes = new DefaultAuthorizationScopeProvider().GetScopeStrings(ctx).ToList();

        Assert.Empty(scopes);
    }

    [Fact]
    public void TopLevelScopeClaim_SpaceSeparated_SplitAndYielded()
    {
        var identity = OtherIdentity(new Claim("scope", "altinn:portal altinn:enterprise altinn:profile"));
        var ctx = MakeContext(new ClaimsPrincipal(identity));

        var scopes = new DefaultAuthorizationScopeProvider().GetScopeStrings(ctx).ToList();

        Assert.Equal(["altinn:portal", "altinn:enterprise", "altinn:profile"], scopes);
    }

    [Fact]
    public void TopLevelScopeClaim_EmptyString_NoScopesYielded()
    {
        var identity = OtherIdentity(new Claim("scope", string.Empty));
        var ctx = MakeContext(new ClaimsPrincipal(identity));

        var scopes = new DefaultAuthorizationScopeProvider().GetScopeStrings(ctx).ToList();

        Assert.Empty(scopes);
    }

    [Fact]
    public void TopLevelScopeClaim_MultipleSpaces_EmptyEntriesRemoved()
    {
        var identity = OtherIdentity(new Claim("scope", "altinn:portal   altinn:profile"));
        var ctx = MakeContext(new ClaimsPrincipal(identity));

        var scopes = new DefaultAuthorizationScopeProvider().GetScopeStrings(ctx).ToList();

        Assert.Equal(["altinn:portal", "altinn:profile"], scopes);
    }

    [Fact]
    public void BothSources_FederationAltinnScope_AndTopLevelScope_AllYielded()
    {
        var identity = FederationIdentity(
            new Claim("urn:altinn:scope", "altinn:portal"),
            new Claim("scope", "altinn:enterprise altinn:profile"));
        var ctx = MakeContext(new ClaimsPrincipal(identity));

        var scopes = new DefaultAuthorizationScopeProvider().GetScopeStrings(ctx).ToList();

        // Federation-identity branch fires first, then the top-level "scope"
        // claim branch picks up the same identity's "scope" claim and splits it.
        Assert.Equal(["altinn:portal", "altinn:enterprise", "altinn:profile"], scopes);
    }
}
