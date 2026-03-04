using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessMgmt.Core.Authorization;

/// <summary>
/// Authorization handler that grants access when the current request matches an access rule
/// and the user has at least one of the required scopes.
/// </summary>
/// <param name="accessor">Accessor used to read the current <see cref="HttpContext"/>.</param>
/// <param name="scopeProvider">Provider used to resolve the user's scope strings.</param>
public class ScopeConditionAuthorizationHandler(IHttpContextAccessor accessor, IAuthorizationScopeProvider scopeProvider) : AuthorizationHandler<ScopeConditionAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeConditionAuthorizationRequirement requirement)
    {
        foreach (var access in requirement.Access.Where(condition => condition.GiveAccess(accessor)))
        {
            if (scopeProvider.GetScopeStrings(context).Any(access.Scopes.Contains))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
