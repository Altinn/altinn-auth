using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessMgmt.Core.Authorization;

/// <summary>
/// Requirement for authorization policies used for validating a client scope if a given condition is true.
/// See policy-based authorization documentation for details.
/// </summary>
public sealed class ScopeConditionAuthorizationRequirement : IAuthorizationRequirement
{
    internal ScopeConditionAuthorizationRequirement(params ConditionalScope[] access)
    {
        Access = [.. access];
    }

    /// <summary>
    /// Gets the access rules for the policy using this requirement.
    /// </summary>
    public List<ConditionalScope> Access { get; }
}

/// <summary>
/// Represents a conditional scope rule:
/// if <see cref="GiveAccess"/> returns true for the current request,
/// then at least one of <see cref="Scopes"/> must be present.
/// </summary>
/// <param name="giveAccess">Predicate that determines whether this rule applies.</param>
/// <param name="scopes">The scopes that satisfy this rule.</param>
public sealed class ConditionalScope(
    Func<IHttpContextAccessor, bool> giveAccess,
    params string[] scopes)
{
    /// <summary>
    /// Gets the scopes that satisfy this rule.
    /// </summary>
    public string[] Scopes { get; } = scopes;

    /// <summary>
    /// Gets the predicate that determines whether this rule applies.
    /// </summary>
    public Func<IHttpContextAccessor, bool> GiveAccess { get; } = giveAccess;

    /// <summary>
    /// Rule that applies when query parameters "party" and "from" are equal.
    /// </summary>
    public static Func<IHttpContextAccessor, bool> ToOthers => QueryParamEquals("party", "from");

    /// <summary>
    /// Rule that applies when query parameters "party" and "to" are equal.
    /// </summary>
    public static Func<IHttpContextAccessor, bool> FromOthers => QueryParamEquals("party", "to");

    /// <summary>
    /// Builds a predicate that returns true when both query parameters exist and are equal (case-insensitive).
    /// </summary>
    public static Func<IHttpContextAccessor, bool> QueryParamEquals(string queryParamA, string queryParamB)
    {
        return accessor =>
        {
            var ctx = accessor.HttpContext;
            if (ctx is null)
            {
                return false;
            }

            if (!ctx.Request.Query.TryGetValue(queryParamA, out var paramA))
            {
                return false;
            }

            if (!ctx.Request.Query.TryGetValue(queryParamB, out var paramB))
            {
                return false;
            }

            return string.Equals(paramA.ToString(), paramB.ToString(), StringComparison.OrdinalIgnoreCase);
        };
    }
}

/// <summary>
/// Describes a scope access requirement in policy-based authorization.
/// </summary>
public interface IDirectionalScopeAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets or sets the scopes defined for the policy using this requirement.
    /// </summary>
    string[] Scope { get; set; }
}
