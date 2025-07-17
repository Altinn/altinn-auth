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
using Swashbuckle.AspNetCore.Annotations;

namespace Altinn.ResourceRegistry.Controllers;

/// <summary>
/// Controller for access lists aggregated memberships APIs.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("resourceregistry/api/v1/access-lists/memberships")]
[NotImplementedFilter]
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
    [Authorize(Policy = AuthzConstants.POLICY_PLATFORM_COMPONENT_ONLY)]
    public async Task<ActionResult<ListObject<AccessListResourceMembershipWithActionFilterDto>>> GetMembershipsForResourceForParty(
        [FromQuery(Name = "party")] List<string?>? partiesQuery = null,
        [FromQuery(Name = "resource")] List<string?>? resourcesQuery = null,
        CancellationToken cancellationToken = default)
    {
        List<PartyUrn>? parties = null;
        List<ResourceUrn>? resources = null;
        ValidationErrorBuilder errors = default;

        if (partiesQuery is { Count: > 0 } pq)
        {
            foreach (var p in pq.SelectMany(s => s?.Split(',') ?? []))
            {
                if (!PartyUrn.TryParse(p, out var party))
                {
                    errors.Add(ValidationErrors.InvalidPartyUrn, "/$QUERY/party", [KeyValuePair.Create("value", p)]);
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
                    errors.Add(ValidationErrors.InvalidResourceUrn, "/$QUERY/resource", [KeyValuePair.Create("value", r)]);
                    continue;
                }

                resources ??= [];
                resources.Add(resource);
            }
        }

        if (parties is not { Count: > 0 })
        {
            errors.Add(ValidationErrors.AccessListMemberships_Requires_Party, "/$QUERY/party");
        }
        else if (parties.Count > 1)
        {
            errors.Add(ValidationErrors.AccessListMemberships_TooManyParties, "/$QUERY/party");
        }

        if (resources is { Count: > 1 })
        {
            errors.Add(ValidationErrors.AccessListMemberships_TooManyResources, "/$QUERY/resource");
        }

        if (errors.TryToActionResult(out var errorResult))
        {
            return errorResult;
        }

        var result = await _service.GetMembershipsForPartiesAndResources(
            parties!,
            resources,
            cancellationToken);

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return ListObject.Create(result.Value.Select(AccessListResourceMembershipWithActionFilterDto.From));
    }

    /// <summary>
    /// Returns a list of access lists for a given member.
    /// </summary>
    /// <param name="memberPartyUUid">The member partyuuid</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>List of accesslist the party is member of</returns>
    [HttpGet("/resourceregistry/api/v1/access-lists/get-by-member")]
    [Authorize(Policy = AuthzConstants.POLICY_PLATFORM_COMPONENT_ONLY)]
    [SwaggerOperation(Tags = ["Access List"])]
    public async Task<ActionResult<IReadOnlyList<AccessListInfoDto>>> GetAccessListsByMember(
        [FromQuery(Name = "party")] PartyUuidUrn memberPartyUUid,
        CancellationToken cancellationToken = default)
    {
        if (memberPartyUUid.IsPartyUuid(out Guid partyUuid))
        {
            IReadOnlyList<AccessListInfo> accesssLists = await _service.GetAccessListsByMember(partyUuid, cancellationToken);
            if (accesssLists == null)
            {
                return NotFound();
            }

            List<AccessListInfoDto> accessListDtos = accesssLists.Select(AccessListInfoDto.From).ToList();

            return Ok(accessListDtos);
        }

        return BadRequest("Invalid partyurn" + memberPartyUUid.ToString());
    }
}
