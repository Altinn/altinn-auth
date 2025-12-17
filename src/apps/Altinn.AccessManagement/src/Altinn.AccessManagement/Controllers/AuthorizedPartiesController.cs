using System.Net.Mime;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
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
[Route("accessmanagement/api/v1/")]
public class AuthorizedPartiesController(
    ILogger<AuthorizedPartiesController> logger,
    IMapper mapper,
    FeatureManager featureManager,
    IAuthorizedPartiesService authorizedPartiesService,
    IContextRetrievalService contextRetrievalService) : ControllerBase
{
    /// <summary>
    /// Endpoint for retrieving all authorized parties (with option to include Authorized Parties, aka Reportees, from Altinn 2) for the authenticated user
    /// </summary>
    /// <param name="includeAltinn2">Optional (Default: False): Whether Authorized Parties from Altinn 2 should be included in the result set, and if access to Altinn 3 resources through having Altinn 2 roles should be included.</param>
    /// <param name="includeAltinn3">Optional (Default: True): Whether Authorized Parties from Altinn 3 should be included in the underlying result set.</param>
    /// <param name="includeRoles">Optional (Default: True): Whether authorized roles should be included in the result set.</param>
    /// <param name="includeAccessPackages">Optional (Default: False): Whether authorized access packages should be included in the result set.</param>
    /// <param name="includeResources">Optional (Default: True): Whether authorized resources should be included in the result set.</param>
    /// <param name="includeInstances">Optional (Default: True): Whether authorized instances should be included in the result set.</param>
    /// <param name="includePartiesViaKeyRoles">Optional (Default: True): Whether authorized parties via organizations the user has a key role for, should be included in the result set.</param>
    /// <param name="includeSubParties">Optional (Default: True): Whether sub-parties of authorized parties should be included in the result set.</param>
    /// <param name="includeInactiveParties">Optional (Default: True): Whether inactive authorized parties should be included in the result set.</param>
    /// <param name="partyFilter">Optional: A list of party uuids to filter the results.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <response code="200" cref="List{AuthorizedParty}">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet]
    [Authorize]
    [Route("authorizedparties")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<AuthorizedPartyExternal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [FeatureGate(FeatureFlags.RightsDelegationApi)]
    public async Task<ActionResult<List<AuthorizedPartyExternal>>> GetAuthorizedParties(
        [FromQuery] bool includeAltinn2 = false,
        [FromQuery] bool includeAltinn3 = true,
        [FromQuery] bool includeRoles = true,
        [FromQuery] bool includeAccessPackages = false,
        [FromQuery] bool includeResources = true,
        [FromQuery] bool includeInstances = true,
        [FromQuery] AuthorizedPartiesIncludeFilter includePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] AuthorizedPartiesIncludeFilter includeSubParties = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] AuthorizedPartiesIncludeFilter includeInactiveParties = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] IEnumerable<Guid>? partyFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filters = new AuthorizedPartiesFilters
            {
                IncludeAltinn2 = includeAltinn2,
                IncludeAltinn3 = includeAltinn3,
                IncludeRoles = includeRoles,
                IncludeAccessPackages = includeAccessPackages,
                IncludeResources = includeResources,
                IncludeInstances = includeInstances,
                IncludePartiesViaKeyRoles = includePartiesViaKeyRoles,
                IncludeSubParties = includeSubParties,
                IncludeInactiveParties = includeInactiveParties,
                PartyFilter = partyFilter?.Distinct().ToDictionary(uuid => uuid, uuid => uuid)
            };

            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesEfEnabled) && partyFilter?.Count() > 0)
            {
                var partyUuids = await authorizedPartiesService.GetPartyFilterUuids(partyFilter, cancellationToken);
                filters.PartyFilter = partyUuids?.Distinct().ToDictionary(uuid => uuid, uuid => uuid);
            }

            int userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == 0)
            {
                return Unauthorized();
            }

            if (userId != 0)
            {
                return mapper.Map<List<AuthorizedPartyExternal>>(await authorizedPartiesService.GetAuthorizedPartiesByUserId(userId, filters, cancellationToken));
            }

            string systemUserUuid = AuthenticationHelper.GetSystemUserUuid(HttpContext);
            if (!string.IsNullOrWhiteSpace(systemUserUuid))
            {
                return mapper.Map<List<AuthorizedPartyExternal>>(await authorizedPartiesService.GetAuthorizedPartiesBySystemUserUuid(systemUserUuid, filters, cancellationToken));
            }

            return Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(500, ex, "Unexpected internal exception occurred during GetAuthorizedParties");
            return new ObjectResult(ProblemDetailsFactory.CreateProblemDetails(HttpContext, detail: "Internal Server Error"));
        }
    }

    /// <summary>
    /// Endpoint for retrieving a given authorized party if it exists (with option to include Authorized Parties, aka Reportees from Altinn 2, when getting the underlying list of authorized parties) in the authenticated user's list of authorized parties
    /// </summary>
    /// <param name="partyId">The partyId to get if exists in the authenticated user's list of authorized parties</param>
    /// <param name="includeAltinn2">Optional (Default: False): Whether Authorized Parties from Altinn 2 should be included in the result set, and if access to Altinn 3 resources through having Altinn 2 roles should be included.</param>
    /// <param name="includeAltinn3">Optional (Default: True): Whether Authorized Parties from Altinn 3 should be included in the underlying result set.</param>
    /// <param name="includeRoles">Optional (Default: True): Whether authorized roles should be included in the result set.</param>
    /// <param name="includeAccessPackages">Optional (Default: False): Whether authorized access packages should be included in the result set.</param>
    /// <param name="includeResources">Optional (Default: True): Whether authorized resources should be included in the result set.</param>
    /// <param name="includeInstances">Optional (Default: True): Whether authorized instances should be included in the result set.</param>
    /// <param name="includePartiesViaKeyRoles">Optional (Default: True): Whether authorized parties via organizations the user has a key role for, should be included in the result set.</param>
    /// <param name="includeSubParties">Optional (Default: True): Whether sub-parties of authorized parties should be included in the result set.</param>
    /// <param name="includeInactiveParties">Optional (Default: True): Whether inactive authorized parties should be included in the result set.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <response code="200" cref="List{AuthorizedParty}">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet]
    [Authorize]
    [Route("authorizedparty/{partyId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<AuthorizedPartyExternal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [FeatureGate(FeatureFlags.RightsDelegationApi)]
    public async Task<ActionResult<AuthorizedPartyExternal>> GetAuthorizedParty(
        [FromRoute] int partyId,
        [FromQuery] bool includeAltinn2 = false,
        [FromQuery] bool includeAltinn3 = true,
        [FromQuery] bool includeRoles = true,
        [FromQuery] bool includeAccessPackages = false,
        [FromQuery] bool includeResources = true,
        [FromQuery] bool includeInstances = true,
        [FromQuery] AuthorizedPartiesIncludeFilter includePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] AuthorizedPartiesIncludeFilter includeSubParties = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] AuthorizedPartiesIncludeFilter includeInactiveParties = AuthorizedPartiesIncludeFilter.Auto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (partyId == 0)
            {
                ModelState.AddModelError("InvalidParty", "The party id must be a valid non-zero integer");
                return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            var filters = new AuthorizedPartiesFilters
            {
                IncludeAltinn2 = includeAltinn2,
                IncludeAltinn3 = includeAltinn3,
                IncludeRoles = includeRoles,
                IncludeAccessPackages = includeAccessPackages,
                IncludeResources = includeResources,
                IncludeInstances = includeInstances,
                IncludePartiesViaKeyRoles = includePartiesViaKeyRoles,
                IncludeSubParties = includeSubParties,
                IncludeInactiveParties = includeInactiveParties,
            };

            int userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == 0)
            {
                return Unauthorized();
            }

            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesEfEnabled))
            {
                var partyFilters = new BaseAttribute(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, partyId.ToString()).SingleToList();
                var partyUuids = await authorizedPartiesService.GetPartyFilterUuids(partyFilters, cancellationToken);
                filters.PartyFilter = partyUuids?.Distinct().ToDictionary(k => k, v => v);
            }

            List<AuthorizedParty> authorizedParties = await authorizedPartiesService.GetAuthorizedPartiesByUserId(userId, filters, cancellationToken);
            AuthorizedParty authorizedParty = authorizedParties.Find(ap => ap.PartyId == partyId && !ap.OnlyHierarchyElementWithNoAccess)
                ?? authorizedParties.SelectMany(ap => ap.Subunits).FirstOrDefault(subunit => subunit.PartyId == partyId);

            if (authorizedParty == null)
            {
                ModelState.AddModelError("InvalidParty", "The party id is either invalid or is not an authorized party for the authenticated user");
                return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            return mapper.Map<AuthorizedPartyExternal>(authorizedParty);
        }
        catch (Exception ex)
        {
            logger.LogError(500, ex, "Unexpected internal exception occurred during GetAuthorizedParties");
            return new ObjectResult(ProblemDetailsFactory.CreateProblemDetails(HttpContext, detail: "Internal Server Error"));
        }
    }

    /// <summary>
    /// Endpoint for retrieving all authorized parties (with option to include Authorized Parties, aka Reportees, from Altinn 2) for the authenticated user
    /// </summary>
    /// <param name="party">The party to retrieve the list of authorized parties for</param>
    /// <param name="includeAltinn2">Optional (Default: False): Whether Authorized Parties from Altinn 2 should be included in the result set, and if access to Altinn 3 resources through having Altinn 2 roles should be included.</param>
    /// <param name="includeAltinn3">Optional (Default: True): Whether Authorized Parties from Altinn 3 should be included in the underlying result set.</param>
    /// <param name="includeRoles">Optional (Default: True): Whether authorized roles should be included in the result set.</param>
    /// <param name="includeAccessPackages">Optional (Default: False): Whether authorized access packages should be included in the result set.</param>
    /// <param name="includeResources">Optional (Default: True): Whether authorized resources should be included in the result set.</param>
    /// <param name="includeInstances">Optional (Default: True): Whether authorized instances should be included in the result set.</param>
    /// <param name="includePartiesViaKeyRoles">Optional (Default: True): Whether authorized parties via organizations the user has a key role for, should be included in the result set.</param>
    /// <param name="includeSubParties">Optional (Default: True): Whether sub-parties of authorized parties should be included in the result set.</param>
    /// <param name="includeInactiveParties">Optional (Default: True): Whether inactive authorized parties should be included in the result set.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <response code="200" cref="List{AuthorizedParty}">Ok</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_READ)]
    [Route("{party}/authorizedparties")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(List<AuthorizedPartyExternal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [FeatureGate(FeatureFlags.RightsDelegationApi)]
    public async Task<ActionResult<List<AuthorizedPartyExternal>>> GetAuthorizedPartiesAsAccessManager(
        [FromRoute] int party,
        [FromQuery] bool includeAltinn2 = false,
        [FromQuery] bool includeAltinn3 = true,
        [FromQuery] bool includeRoles = true,
        [FromQuery] bool includeAccessPackages = false,
        [FromQuery] bool includeResources = true,
        [FromQuery] bool includeInstances = true,
        [FromQuery] AuthorizedPartiesIncludeFilter includePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.True,
        [FromQuery] AuthorizedPartiesIncludeFilter includeSubParties = AuthorizedPartiesIncludeFilter.True,
        [FromQuery] AuthorizedPartiesIncludeFilter includeInactiveParties = AuthorizedPartiesIncludeFilter.True,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filters = new AuthorizedPartiesFilters
            {
                IncludeAltinn2 = includeAltinn2,
                IncludeAltinn3 = includeAltinn3,
                IncludeRoles = includeRoles,
                IncludeAccessPackages = includeAccessPackages,
                IncludeResources = includeResources,
                IncludeInstances = includeInstances,
                IncludePartiesViaKeyRoles = includePartiesViaKeyRoles,
                IncludeSubParties = includeSubParties,
                IncludeInactiveParties = includeInactiveParties
            };

            int authenticatedUserPartyId = AuthenticationHelper.GetPartyId(HttpContext);

            Party subject = await contextRetrievalService.GetPartyAsync(party, cancellationToken);
            if (subject.PartyTypeName == PartyType.Person && subject.PartyId != authenticatedUserPartyId)
            {
                return Forbid();
            }

            List<AuthorizedParty> authorizedParties = await authorizedPartiesService.GetAuthorizedPartiesByPartyId(subject.PartyId, filters, cancellationToken);

            return mapper.Map<List<AuthorizedPartyExternal>>(authorizedParties);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("Argument exception", ex.Message);
            return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
        }
        catch (Exception ex)
        {
            logger.LogError(500, ex, "Unexpected internal exception occurred during GetAuthorizedParties");
            return new ObjectResult(ProblemDetailsFactory.CreateProblemDetails(HttpContext, detail: "Internal Server Error"));
        }
    }
}
