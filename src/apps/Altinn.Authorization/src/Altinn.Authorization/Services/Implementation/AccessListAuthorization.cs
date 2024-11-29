using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.Enums;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Services.Interface;

namespace Altinn.Platform.Authorization.Services.Implementation;

/// <summary>
/// The service used to map internal delegation change to delegation change events and push them to the event queue.
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

        if (memberships == null || !memberships.Any())
        {
            response.Result = AccessListAuthorizationResult.NotAuthorized;
        }
        else if (memberships.Any(m => m.ActionFilters == null || m.ActionFilters.Any(actionFilter => actionFilter == request.Action.Value.ValueSpan.ToString())))
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
