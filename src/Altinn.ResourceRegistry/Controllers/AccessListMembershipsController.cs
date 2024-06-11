#nullable enable

using Altinn.Authorization.ProblemDetails;
using Altinn.ResourceRegistry.Auth;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Filters;
using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Controllers;

/// <summary>
/// Controller for access lists aggregated memberships APIs.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("resourceregistry/api/v1/access-lists/memberships")]
[NotImplementedFilter]
[ResourceOwnerFromRouteValue("owner")]
[Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_READ)]
public class AccessListMembershipsController
    : ControllerBase
{
    private readonly IAccessListService _service;

    /// <summary>
    /// Constructs a new <see cref="AccessListMembershipsController"/>.
    /// </summary>
    public AccessListMembershipsController(IAccessListService service)
    {
        _service = service;
    }

    /// <summary>
    /// <para>
    ///   Gets memberships for a party for a set of resources/parties.
    /// </para>
    /// <para>
    ///   This is an internal API and requires an administrative token.
    /// </para>
    /// </summary>
    /// <param name="partiesQuery">Parties to include.</param>
    /// <param name="resourcesQuery">Resources to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list-object of access-list memberships that include <paramref name="partiesQuery"/> and <paramref name="resourcesQuery"/>.</returns>
    [HttpGet("")]
    [Authorize(Policy = AuthzConstants.POLICY_ADMIN)]
    public async Task<ActionResult<ListObject<AccessListResourceMembershipDto>>> GetMembershipsForResourceForParty(
        [FromQuery(Name = "party")] List<string?>? partiesQuery = null,
        [FromQuery(Name = "resource")] List<string?>? resourcesQuery = null,
        CancellationToken cancellationToken = default)
    {
        List<PartyUrn>? parties = null;
        List<ResourceUrn>? resources = null;
        List<AltinnValidationError>? errors = null;

        if (partiesQuery is { Count: > 0 } pq)
        {
            foreach (var p in pq.SelectMany(s => s?.Split(',') ?? []))
            {
                if (!PartyUrn.TryParse(p, out var party))
                {
                    errors ??= [];
                    errors.Add(ValidationErrors.InvalidPartyUrn.ToValidationError("/$QUERY/party", [KeyValuePair.Create("value", (object?)p)]));
                    continue;
                }

                parties ??= [];
                parties.Add(party);
            }
        }

        if (resourcesQuery is { Count: > 0 } rq)
        {
            foreach (var r in rq.SelectMany(s => s?.Split(',') ?? []))
            {
                if (!ResourceUrn.TryParse(r, out var resource))
                {
                    errors ??= [];
                    errors.Add(ValidationErrors.InvalidResourceUrn.ToValidationError("/$QUERY/resource", [KeyValuePair.Create("value", (object?)r)]));
                    continue;
                }

                resources ??= [];
                resources.Add(resource);
            }
        }

        if (parties is not { Count: > 0 })
        {
            errors ??= [];
            errors.Add(ValidationErrors.AccessListMemberships_Requires_Party.ToValidationError("/$QUERY/party"));
        }
        else if (parties.Count > 1)
        {
            errors ??= [];
            errors.Add(ValidationErrors.AccessListMemberships_TooManyParties.ToValidationError("/$QUERY/party"));
        }

        if (resources is { Count: > 1 })
        {
            errors ??= [];
            errors.Add(ValidationErrors.AccessListMemberships_TooManyResources.ToValidationError("/$QUERY/resource"));
        }

        if (errors is { Count: > 0 })
        {
            var problem = new AltinnValidationProblemDetails(errors);
            return problem.ToActionResult();
        }

        var result = await _service.GetMembershipsForPartiesAndResources(
            parties!,
            resources,
            cancellationToken);

        if (result.IsProblem(out var problemResult))
        {
            return problemResult.ToActionResult();
        }

        return ListObject.Create(result.Value.Select(AccessListResourceMembershipDto.From));
    }
}
