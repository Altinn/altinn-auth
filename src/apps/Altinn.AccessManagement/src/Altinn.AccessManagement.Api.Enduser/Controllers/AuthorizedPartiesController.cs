using System.Net.Mime;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller responsible for all operations for retrieving AuthorizedParties for an authenticated user
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/authorizedparties")]
public class AuthorizedPartiesController(
    ILogger<AuthorizedPartiesController> logger,
    FeatureManager featureManager,
    IAuthorizedPartiesService authorizedPartiesService) : ControllerBase
{
    /// <summary>
    /// Endpoint for retrieving all authorized parties for the authenticated user
    /// </summary>
    /// <param name="includeRoles">Optional (Default: True): Whether authorized roles should be included in the result set.</param>
    /// <param name="includeAccessPackages">Optional (Default: False): Whether authorized access packages should be included in the result set.</param>
    /// <param name="includeResources">Optional (Default: True): Whether authorized resources should be included in the result set.</param>
    /// <param name="includeInstances">Optional (Default: True): Whether authorized instances should be included in the result set.</param>
    /// <param name="includePartiesViaKeyRoles">Optional (Default: True): Whether authorized parties via organizations the user has a key role for, should be included in the result set.</param>
    /// <param name="includeSubParties">Optional (Default: True): Whether sub-parties of authorized parties should be included in the result set.</param>
    /// <param name="includeInactiveParties">Optional (Default: True): Whether inactive authorized parties should be included in the result set.</param>
    /// <param name="partyFilter">Optional: A list of party uuids to filter the results.</param>
    /// <param name="anyOfResourceIds">Optional: Filter for only returning authorized parties where the subject has access to any of the provided resource ids. Invalid resource ids are ignored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_AUTHORIZEDPARTIES)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(PaginatedResult<List<AuthorizedPartyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEnduserAuthorizedParties(
        [FromQuery] bool includeRoles = false,
        [FromQuery] bool includeAccessPackages = false,
        [FromQuery] bool includeResources = false,
        [FromQuery] bool includeInstances = false,
        [FromQuery] AuthorizedPartiesIncludeFilter includePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] AuthorizedPartiesIncludeFilter includeSubParties = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] AuthorizedPartiesIncludeFilter includeInactiveParties = AuthorizedPartiesIncludeFilter.Auto,
        [FromQuery] IEnumerable<Guid>? partyFilter = null,
        [FromQuery] string[] anyOfResourceIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filters = new AuthorizedPartiesFilters
            {
                IncludeAltinn2 = true,
                IncludeAltinn3 = true,
                IncludeRoles = includeRoles,
                IncludeAccessPackages = includeAccessPackages,
                IncludeResources = includeResources,
                IncludeInstances = includeInstances,
                IncludePartiesViaKeyRoles = includePartiesViaKeyRoles,
                IncludeSubParties = includeSubParties,
                IncludeInactiveParties = includeInactiveParties,
                AnyOfResourceIds = anyOfResourceIds
            };

            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesEfEnabled, cancellationToken) && partyFilter?.Any() == true)
            {
                var partyUuids = await authorizedPartiesService.GetPartyFilterUuids(partyFilter, cancellationToken);
                filters.PartyFilter = new SortedDictionary<Guid, Guid>();

                foreach (var partyUuid in partyUuids.Distinct())
                {
                    filters.PartyFilter[partyUuid] = partyUuid;
                }
            }

            int userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId != 0)
            {
                var result = await authorizedPartiesService.GetAuthorizedPartiesByUserId(userId, filters, cancellationToken);
                return Ok(PaginatedResult.Create(DtoMapper.ConvertToAuthorizedPartiesDto(result), null));
            }

            string systemUserUuid = AuthenticationHelper.GetSystemUserUuid(HttpContext);
            if (!string.IsNullOrWhiteSpace(systemUserUuid))
            {
                var result = await authorizedPartiesService.GetAuthorizedPartiesBySystemUserUuid(systemUserUuid, filters, cancellationToken);
                return Ok(PaginatedResult.Create(DtoMapper.ConvertToAuthorizedPartiesDto(result), null));
            }

            //// ToDo: support self-identified email-users 

            return Unauthorized("Unknown user type");
        }
        catch (Exception ex)
        {
            logger.LogError(500, ex, "Unexpected internal exception occurred during GetEnduserAuthorizedParties");
            return new ObjectResult(ProblemDetailsFactory.CreateProblemDetails(HttpContext, detail: "Internal Server Error"));
        }
    }
}
