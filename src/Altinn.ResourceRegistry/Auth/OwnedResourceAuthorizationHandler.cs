#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Core.ServiceOwners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Altinn.ResourceRegistry.Auth;

/// <summary>
/// Authorization handler for checking if the user owns the resource.
/// </summary>
internal class OwnedResourceAuthorizationHandler
    : AuthorizationHandler<UserOwnsResourceRequirement>
{
    private readonly IServiceOwnerService _serviceOwnerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OwnedResourceAuthorizationHandler"/> class.
    /// </summary>
    public OwnedResourceAuthorizationHandler(IServiceOwnerService serviceOwnerService)
    {
        _serviceOwnerService = serviceOwnerService;
    }

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
        if (providers is null)
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

        if (resourceOwner is not null)
        {
            var matchesTask = MatchResourceOwner(context.User, resourceOwner, httpContext.RequestAborted);
            if (matchesTask.IsCompletedSuccessfully)
            {
                #pragma warning disable VSTHRD103 // Call async methods when in an async method
                return HandleMatch(context, requirement, matchesTask.Result);
                #pragma warning restore VSTHRD103 // Call async methods when in an async method
            }
            else
            {
                return AwaitMatch(context, requirement, matchesTask);
            }
        }

        return Task.CompletedTask;

        static async Task AwaitMatch(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement, ValueTask<bool> matchesTask)
            => await HandleMatch(context, requirement, await matchesTask);

        static Task HandleMatch(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement, bool matches)
        {
            if (matches)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    private Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement, IHasResourceOwner resource)
    {
        var matchesTask = MatchResourceOwner(context.User, resource.ResourceOwner, CancellationToken.None);
        if (matchesTask.IsCompletedSuccessfully)
        {
            #pragma warning disable VSTHRD103 // Call async methods when in an async method
            return HandleMatch(context, requirement, matchesTask.Result);
            #pragma warning restore VSTHRD103 // Call async methods when in an async method
        }
        else
        {
            return AwaitMatch(context, requirement, matchesTask);
        }

        static async Task AwaitMatch(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement, ValueTask<bool> matchesTask)
            => await HandleMatch(context, requirement, await matchesTask);

        static Task HandleMatch(AuthorizationHandlerContext context, UserOwnsResourceRequirement requirement, bool matches)
        {
            if (matches)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    private ValueTask<bool> MatchResourceOwner(ClaimsPrincipal user, string resourceOwner, CancellationToken cancellationToken)
    {
        if (OrganizationNumber.TryParse(resourceOwner, provider: null, out var orgNo))
        {
            return new(MatchOrganizationResourceOwner(user, orgNo));
        }

        var serviceOwnerClaim = user.FindFirst("urn:altinn:org")?.Value;
        if (string.IsNullOrEmpty(serviceOwnerClaim))
        {
            if (!TryGetOrganizationNumberFromUser(user, out var userOrgNo))
            {
                return new(false);
            }

            var serviceOwnersTask = _serviceOwnerService.GetServiceOwners(cancellationToken);
            if (serviceOwnersTask.IsCompletedSuccessfully)
            {
                #pragma warning disable VSTHRD103 // Call async methods when in an async method
                return new(MatchServiceOwnerByOrganizationNumber(serviceOwnersTask.Result, userOrgNo, resourceOwner));
                #pragma warning restore VSTHRD103 // Call async methods when in an async method
            }

            return AwaitMatchServiceOwnerByOrganizationNumber(serviceOwnersTask, userOrgNo, resourceOwner);
        }

        return new(string.Equals(serviceOwnerClaim, resourceOwner, StringComparison.OrdinalIgnoreCase));

        static async ValueTask<bool> AwaitMatchServiceOwnerByOrganizationNumber(
            ValueTask<ServiceOwnerLookup> serviceOwnerLookupTask,
            OrganizationNumber userOrgNo,
            string resourceOwner)
            => MatchServiceOwnerByOrganizationNumber(await serviceOwnerLookupTask, userOrgNo, resourceOwner);

        static bool MatchServiceOwnerByOrganizationNumber(
            ServiceOwnerLookup serviceOwners,
            OrganizationNumber userOrgNo,
            string resourceOwner)
        {
            if (serviceOwners.TryFind(userOrgNo, out var matching))
            {
                foreach (var so in matching)
                {
                    if (string.Equals(so.OrgCode, resourceOwner, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private static bool MatchOrganizationResourceOwner(ClaimsPrincipal user, OrganizationNumber orgNo)
    {
        return TryGetOrganizationNumberFromUser(user, out var userOrgNo) && userOrgNo == orgNo;
    }

    private static bool TryGetOrganizationNumberFromUser(ClaimsPrincipal user, [NotNullWhen(true)] out OrganizationNumber? orgNo)
    {
        orgNo = null;
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

        var orgNoStr = claim.Id.AsSpan(5);
        return OrganizationNumber.TryParse(orgNoStr, provider: null, out orgNo);
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
