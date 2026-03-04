using Altinn.AccessMgmt.Core.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Altinn.AccessMgmt.Core.Extensions;

public static class AuthorizationRequirementsExtensions
{
    public static AuthorizationPolicyBuilder AddRequirementConditionalScope(this AuthorizationPolicyBuilder builder, params ConditionalScope[] access)
    {
        builder.Requirements.Add(new ScopeConditionAuthorizationRequirement(access));
        return builder;
    }
}
