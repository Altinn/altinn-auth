using System.Net.Mime;
using Altinn.AccessManagement.Api.Enterprise.Utils;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
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
public class ResourceOwnerAuthorizedPartiesController(ILogger<ResourceOwnerAuthorizedPartiesController> logger, IMapper mapper, FeatureManager featureManager, IAuthorizedPartiesService authorizedPartiesService, IProviderService providerService) : ControllerBase
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
    /// <param name="includePartiesViaKeyRoles">Optional (Default: True): Whether authorized parties via organizations the user has a key role for, should be included in the result set.</param>
    /// <param name="includeSubParties">Optional (Default: True): Whether sub-parties of authorized parties should be included in the result set.</param>
    /// <param name="includeInactiveParties">Optional (Default: True): Whether inactive authorized parties should be included in the result set.</param>
    /// <param name="orgCode">Optional: Filter for only returning authorized parties where the subject has access to any resource owned by a specific service owner identified by the org code.</param>
    /// <param name="anyOfResourceIds">Optional: Filter for only returning authorized parties where the subject has access to any of the provided resource ids. Invalid resource ids are ignored.</param>
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
        [FromQuery] AuthorizedPartiesIncludeFilter includePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.True,
        [FromQuery] AuthorizedPartiesIncludeFilter includeSubParties = AuthorizedPartiesIncludeFilter.True,
        [FromQuery] AuthorizedPartiesIncludeFilter includeInactiveParties = AuthorizedPartiesIncludeFilter.True,
        [FromQuery] string orgCode = null,
        [FromQuery] string[] anyOfResourceIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            BaseAttribute subjectAttribute = new BaseAttribute(subject.Type, subject.Value);

            if (orgCode != null)
            {
                orgCode = orgCode.Trim().ToLower();
                if (! await IsAuthorizedForOrgCode(orgCode, cancellationToken))
                {
                    ModelState.AddModelError(orgCode, $"Authenticated service owner organization is not authorized/owner of org code: {orgCode}.");
                    return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
                }
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
                ProviderCode = orgCode,
                AnyOfResourceIds = anyOfResourceIds
            };

            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesEfEnabled) && subject.PartyFilter?.Count() > 0)
            {
                var partyFilters = subject.PartyFilter.Select(attr => new BaseAttribute(attr.Type, attr.Value)).ToList();
                var partyUuids = await authorizedPartiesService.GetPartyFilterUuids(partyFilters, cancellationToken);
                filters.PartyFilter = new SortedDictionary<Guid, Guid>();
                foreach (var partyUuid in partyUuids?.Distinct())
                {
                    filters.PartyFilter[partyUuid] = partyUuid;
                }
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

    private async Task<bool> IsAuthorizedForOrgCode(string orgCode, CancellationToken ct)
    {
        var isAdmin = User.FindFirst("scope")?.Value.Contains(AuthzConstants.SCOPE_AUTHORIZEDPARTIES_ADMIN);
        if (isAdmin == true)
        {
            return true;
        }

        var tokenOrgCode = User.FindFirst("urn:altinn:org")?.Value;
        if (!string.IsNullOrEmpty(tokenOrgCode) && tokenOrgCode.Equals(orgCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var consumerUrn = OrgUtil.GetAuthenticatedParty(User);
        if (consumerUrn.IsOrganizationId(out var organizationId))
        {
            Provider provider = await providerService.GetProviderByOrganizationId(organizationId.ToString(), ct);
            if (orgCode.Equals(provider?.Code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
