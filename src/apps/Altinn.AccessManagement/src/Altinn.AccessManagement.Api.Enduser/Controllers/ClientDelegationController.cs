using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Utils;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

[ApiController]
[Route("accessmanagement/api/v1/enduser/clientdelegations")]
[FeatureGate(AccessMgmtFeatureFlags.EnduserControllerClientDelegation)]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
[Tags("Client Delegation")]
public class ClientDelegationController(
    IClientDelegationService clientDelegationService,
    IUserProfileLookupService UserProfileLookupService,
    IEntityService EntityService) : ControllerBase
{
    [HttpGet("clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.ClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClients(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetClientsAsync(party, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.ClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAgents(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetAgentsAsync(party, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpPost("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddAgent(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "to")] Guid? to,
        [FromBody] PersonInput? person,
        CancellationToken cancellationToken = default)
    {
        bool hasPersonInputParameter = person is { };

        var validationErrors = ValidationComposer.Validate(
            ValidationComposer.Any(
                ConnectionValidation.ValidateAddAssignmentWithPersonInput(person.PersonIdentifier, person.LastName),
                ConnectionParameterRules.ToIsGuid(to)
            )
        );

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var resolver = new ToUuidResolver(EntityService, UserProfileLookupService);
        var resolveResult = hasPersonInputParameter
            ? await resolver.ResolveWithPersonInputAsync(person, HttpContext, cancellationToken)
            : await resolver.ResolveWithConnectionInputAsync((Guid)to, false, cancellationToken);

        if (!resolveResult.Success)
        {
            return resolveResult.ErrorResult!;
        }

        var result = await clientDelegationService.AddAgent(party, resolveResult.ToUuid, cancellationToken);

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpDelete("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveAgent(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "to")][Required] Guid to,
        CancellationToken cancellationToken = default)
    {
        await clientDelegationService.RemoveAgent(party, to, cancellationToken);
        return NoContent();
    }

    [HttpGet("agents/accesspackages")]
    public IActionResult GetDelegatedAccessPackagesToAgentsViaParty(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "to")][Required] Guid to,
        CancellationToken cancellationToken = default)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet("clients/accesspackages")]
    public IActionResult GetDelegatedAccessPackagesFromClientsViaParty(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "from")][Required] Guid from,
        CancellationToken cancellationToken = default
    )
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPost("agents/accesspackages")]
    public IActionResult AddAgentAccessPackage(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "from")][Required] Guid from,
        [FromQuery(Name = "to")][Required] Guid to,
        [FromQuery(Name = "packageId")] Guid? packageId,
        [FromQuery(Name = "package")] string package,
        CancellationToken cancellationToken = default)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpDelete("agents/accesspackages")]
    public IActionResult DeleteAgentAccessPackage(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "from")][Required] Guid from,
        [FromQuery(Name = "to")][Required] Guid to,
        [FromQuery(Name = "packageId")] Guid? packageId,
        [FromQuery(Name = "package")] string package,
        CancellationToken cancellationToken = default
    )
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
