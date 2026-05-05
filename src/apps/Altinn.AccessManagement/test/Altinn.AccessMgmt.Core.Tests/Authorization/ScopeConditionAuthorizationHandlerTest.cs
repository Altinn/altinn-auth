using Altinn.AccessMgmt.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

// See: overhaul part-2 step 13
namespace Altinn.AccessMgmt.Core.Tests.Authorization;

/// <summary>
/// Pure-unit tests for <see cref="ScopeConditionAuthorizationHandler"/> — the
/// AuthorizationHandler that grants access when an access rule's predicate
/// matches the current request *and* the user has at least one of the rule's
/// required scopes.
/// </summary>
public class ScopeConditionAuthorizationHandlerTest
{
    private sealed class StubAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }

    private sealed class StubScopeProvider(IEnumerable<string> scopes) : IAuthorizationScopeProvider
    {
        public IEnumerable<string> GetScopeStrings(AuthorizationHandlerContext context) => scopes;
    }

    private static AuthorizationHandlerContext MakeContext(
        ScopeConditionAuthorizationRequirement requirement,
        ClaimsPrincipal? user = null) =>
        new([requirement], user ?? new ClaimsPrincipal(new ClaimsIdentity()), null);

    private static IHttpContextAccessor AccessorWithQuery(string queryString)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString(queryString);
        return new StubAccessor(ctx);
    }

    private static ScopeConditionAuthorizationRequirement Requirement(params ConditionalScope[] access) =>
        (ScopeConditionAuthorizationRequirement)Activator.CreateInstance(
            typeof(ScopeConditionAuthorizationRequirement),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: [access],
            culture: null)!;

    [Fact]
    public async Task PredicateTrue_UserHasMatchingScope_Succeeds()
    {
        var accessor = AccessorWithQuery("?party=urn:1&from=urn:1");
        var requirement = Requirement(new ConditionalScope(ConditionalScope.ToOthers, "scope:a", "scope:b"));
        var ctx = MakeContext(requirement);
        var handler = new ScopeConditionAuthorizationHandler(accessor, new StubScopeProvider(["scope:b"]));

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task PredicateTrue_UserLacksScope_DoesNotSucceed()
    {
        var accessor = AccessorWithQuery("?party=urn:1&from=urn:1");
        var requirement = Requirement(new ConditionalScope(ConditionalScope.ToOthers, "scope:a"));
        var ctx = MakeContext(requirement);
        var handler = new ScopeConditionAuthorizationHandler(accessor, new StubScopeProvider(["scope:other"]));

        await handler.HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task PredicateFalse_UserHasScope_DoesNotSucceed()
    {
        // ToOthers requires party == from; query has party != from → predicate false
        var accessor = AccessorWithQuery("?party=urn:1&from=urn:2");
        var requirement = Requirement(new ConditionalScope(ConditionalScope.ToOthers, "scope:a"));
        var ctx = MakeContext(requirement);
        var handler = new ScopeConditionAuthorizationHandler(accessor, new StubScopeProvider(["scope:a"]));

        await handler.HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task NoMatchingRule_UserHasScope_DoesNotSucceed()
    {
        var accessor = AccessorWithQuery("?party=urn:1");
        var requirement = Requirement(); // empty access list
        var ctx = MakeContext(requirement);
        var handler = new ScopeConditionAuthorizationHandler(accessor, new StubScopeProvider(["scope:a"]));

        await handler.HandleAsync(ctx);

        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public async Task FirstRuleMatchesWithScope_Succeeds_WithoutEvaluatingSecondRule()
    {
        var accessor = AccessorWithQuery("?party=urn:1&from=urn:1");

        bool secondPredicateInvoked = false;
        var requirement = Requirement(
            new ConditionalScope(ConditionalScope.ToOthers, "scope:a"),
            new ConditionalScope(_ => { secondPredicateInvoked = true; return true; }, "scope:other"));

        var ctx = MakeContext(requirement);
        var handler = new ScopeConditionAuthorizationHandler(accessor, new StubScopeProvider(["scope:a"]));

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);

        // Important: the handler returns Task.CompletedTask after the first
        // successful match — pinning early-exit so a second rule's side
        // effects (db lookups, log calls, etc.) don't fire unnecessarily.
        Assert.False(secondPredicateInvoked);
    }

    [Fact]
    public async Task FirstRuleNoMatch_SecondRuleMatchesWithScope_Succeeds()
    {
        var accessor = AccessorWithQuery("?party=urn:1&to=urn:1"); // FromOthers matches; ToOthers does not
        var requirement = Requirement(
            new ConditionalScope(ConditionalScope.ToOthers, "scope:a"),
            new ConditionalScope(ConditionalScope.FromOthers, "scope:b"));

        var ctx = MakeContext(requirement);
        var handler = new ScopeConditionAuthorizationHandler(accessor, new StubScopeProvider(["scope:b"]));

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }
}
