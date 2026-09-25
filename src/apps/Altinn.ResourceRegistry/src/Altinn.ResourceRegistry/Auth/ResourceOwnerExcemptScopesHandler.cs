#nullable enable

using System.Security.Claims;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Altinn.ResourceRegistry.Auth;

/// <summary>
/// Authorization handler for checking if the user is an admin.
/// </summary>
internal class ResourceOwnerExcemptScopesHandler
    : AuthorizationHandler<UserOwnsResourceRequirement>
{
    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement)
    {
        if (IsAdmin(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        var altinScopes = user.Identities
            .Where(i => i.AuthenticationType == "AuthenticationTypes.Federation")
            .SelectMany(i => i.FindAll("urn:altinn:scope"));

        var normalScopes = user.FindAll("scope");

        var scopes = altinScopes.Concat(normalScopes)
            .Select(claim => claim.Value)
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .SelectMany(claim => claim.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        return scopes.ContainsAnyOf([AuthzConstants.SCOPE_RESOURCE_ADMIN, AuthzConstants.SCOPE_ACCESS_LIST_PDP]);
    }
}
