using System.Net.Mime;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessMgmt.Core;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Controllers;

/// <summary>
/// Controller responsible for all operations for retrieving AuthorizedParties list for a user / organization / system
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/resourceowner")]
public class ResourceOwnerAuthorizedPartiesController(ILogger<ResourceOwnerAuthorizedPartiesController> logger, IMapper mapper, FeatureManager featureManager, IAuthorizedPartiesService authorizedPartiesService) : ControllerBase
{
    /// <summary>
    /// Endpoint for retrieving all authorized parties (with option to include Authorized Parties, aka Reportees, from Altinn 2) for a given user or organization 
    /// </summary>
    /// <param name="subject">Subject input model identifying the user or organization to retrieve the list of authorized parties for</param>
    /// <param name="includeAltinn2">Optional (Default: False): Whether Authorized Parties from Altinn 2 should be included in the result set, and if access to Altinn 3 resources through having Altinn 2 roles should be included.</param>
    /// <param name="includeAltinn3">Optional (Default: True): Whether Authorized Parties from Altinn 3 should be included in the underlying result set.</param>
    /// <param name="includeRoles">Optional (Default: True): Whether authorized roles should be included in the result set.</param>
    /// <param name="includeAccessPackages">Optional (Default: False): Whether authorized access packages should be included in the result set.</param>
    /// <param name="includeResources">Optional (Default: True): Whether authorized resources should be included in the result set.</param>
    /// <param name="includeInstances">Optional (Default: True): Whether authorized instances should be included in the result set.</param>
    /// <param name="includePartiesViaKeyRoles">Optional (Default: True): Whether authorized parties via organizations the user has a key role for, should be included in the result set. Note: incomplete implementation (only affects access packages from Altinn 3)</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <response code="200" cref="List{AuthorizedParty}">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_RESOURCEOWNER_AUTHORIZEDPARTIES)]
    [Route("authorizedparties")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<AuthorizedPartyExternal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [FeatureGate(FeatureFlags.RightsDelegationApi)]
    public async Task<ActionResult<List<AuthorizedPartyExternal>>> GetAuthorizedPartiesAsServiceOwner(
        [FromBody] AuthorizedPartyRequest subject,
        [FromQuery] bool includeAltinn2 = false,
        [FromQuery] bool includeAltinn3 = true,
        [FromQuery] bool includeRoles = true,
        [FromQuery] bool includeAccessPackages = false,
        [FromQuery] bool includeResources = true,
        [FromQuery] bool includeInstances = true,
        [FromQuery] bool includePartiesViaKeyRoles = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            BaseAttribute subjectAttribute = new BaseAttribute(subject.Type, subject.Value);

            var filters = new AuthorizedPartiesFilters
            {
                IncludeAltinn2 = includeAltinn2,
                IncludeAltinn3 = includeAltinn3,
                IncludeRoles = includeRoles,
                IncludeAccessPackages = includeAccessPackages,
                IncludeResources = includeResources,
                IncludeInstances = includeInstances,
                IncludePartiesViaKeyRoles = includePartiesViaKeyRoles
            };

            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesEfEnabled) && subject.PartyFilter?.Count() > 0)
            {
                var partyFilters = subject.PartyFilter.Select(attr => new BaseAttribute(attr.Type, attr.Value)).ToList();
                var partyUuids = await authorizedPartiesService.GetPartyFilterUuids(partyFilters, cancellationToken);
                filters.PartyFilter = partyUuids?.Distinct().ToDictionary(k => k, v => v);
            }

            List<AuthorizedParty> authorizedParties = await authorizedPartiesService.GetAuthorizedParties(subjectAttribute, filters, cancellationToken);
            return mapper.Map<List<AuthorizedPartyExternal>>(authorizedParties);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("Argument exception", ex.Message);
            return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
        }
        catch (Exception ex)
        {
            logger.LogError(500, ex, "Unexpected internal exception occurred during ServiceOwnerGetAuthorizedParties");
            return new ObjectResult(ProblemDetailsFactory.CreateProblemDetails(HttpContext, detail: "Internal Server Error"));
        }
    }
}
