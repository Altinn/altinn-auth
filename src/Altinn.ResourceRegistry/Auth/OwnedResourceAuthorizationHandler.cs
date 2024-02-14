#nullable enable

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;

namespace Altinn.ResourceRegistry.Auth;

/// <summary>
/// Authorization handler for checking if the user owns the resource.
/// </summary>
internal class OwnedResourceAuthorizationHandler
    : AuthorizationHandler<UserOwnsResourceRequirement>
{
    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext)
        {
            return HandleRequirementAsync(context, requirement, httpContext);
        }

        if (context.Resource is IHasResourceOwner resource)
        {
            return HandleRequirementAsync(context, requirement, resource);
        }

        return Task.CompletedTask;
    }

    private Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement, HttpContext httpContext)
    {
        var providers = httpContext.GetEndpoint()?.Metadata.GetOrderedMetadata<IResourceOwnerProvider<HttpContext>>();
        if (providers == null)
        {
            return Task.CompletedTask;
        }

        string? resourceOwner = null;
        foreach (var provider in providers)
        {
            if (provider.TryGetResourceOwner(httpContext, out resourceOwner))
            {
                break;
            }
        }

        if (resourceOwner is not null && MatchResourceOwner(context.User, resourceOwner))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement, IHasResourceOwner resource)
    {
        if (MatchResourceOwner(context.User, resource.ResourceOwner))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool MatchResourceOwner(ClaimsPrincipal user, string resourceOwner)
    {
        var orgClaim = user.FindFirst("consumer")?.Value;
        if (string.IsNullOrEmpty(orgClaim))
        {
            return false;
        }

        var claim = JsonSerializer.Deserialize<ConsumerClaim>(orgClaim);
        if (claim is null)
        {
            return false;
        }

        if (claim.Authority != "iso6523-actorid-upis")
        {
            return false;
        }

        if (string.IsNullOrEmpty(claim.Id) || !claim.Id.StartsWith("0192:"))
        {
            return false;
        }

        var orgNr = claim.Id.AsSpan(5);
        return orgNr.SequenceEqual(resourceOwner.AsSpan());
    }

    /// <summary>
    /// The consumer claim object
    /// </summary>
    private sealed class ConsumerClaim
    {
        /// <summary>
        /// Gets or sets the format of the identifier. Must always be "iso6523-actorid-upis"
        /// </summary>
        [JsonPropertyName("authority")]
        public string? Authority { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the consumer. Must have ISO6523 prefix, which should be "0192:" for norwegian organization numbers
        /// </summary>
        [JsonPropertyName("ID")]
        public string? Id { get; set; }
    }
}
