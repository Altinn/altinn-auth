using Altinn.Authorization.Enums;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Services.Interface;

namespace Altinn.Platform.Authorization.Services.Implementation;

/// <summary>
/// Service for authorizing a request against the access list memberships of a party for a resource, including any action filters.
/// </summary>
public class AccessListAuthorization : IAccessListAuthorization
{
    private readonly IResourceRegistry _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessListAuthorization"/> class.
    /// </summary>
    public AccessListAuthorization(IResourceRegistry client)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public async Task<Result<AccessListAuthorizationResponse>> Authorize(AccessListAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        AccessListAuthorizationResponse response = AccessListAuthorizationResponse.From(request);
        IEnumerable<AccessListResourceMembershipWithActionFilterDto> memberships = await _client.GetMembershipsForResourceForParty(request.Subject.Value, request.Resource.Value, cancellationToken);

        string action = request.Action.Value.ValueSpan.ToString();

        if (memberships == null || !memberships.Any())
        {
            response.Result = AccessListAuthorizationResult.NotAuthorized;
        }
        else if (memberships.Any(m => m.ActionFilters == null || m.ActionFilters.Any(actionFilter => string.Equals(actionFilter, action, StringComparison.OrdinalIgnoreCase))))
        {
            response.Result = AccessListAuthorizationResult.Authorized;
        }
        else
        {
            response.Result = AccessListAuthorizationResult.NotAuthorized;
        }

        return new(response);
    }
}
