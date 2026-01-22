using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Threading.Tasks;
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
[Tags("Client Delegation")]
public class ClientDelegationController(
    IClientDelegationService clientDelegationService,
    IUserProfileLookupService UserProfileLookupService,
    IEntityService EntityService) : ControllerBase
{
    [HttpGet("clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.ClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClients(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetClientsForPartyAsync(party, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.ClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAgents(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetAgentsForPartyAsync(party, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpPost("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
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
                ConnectionValidation.ValidateAddAssignmentWithPersonInput(person?.PersonIdentifier, person?.LastName),
                ParameterValidation.ToIsGuid(to)
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

        var result = await clientDelegationService.AddAgentForParty(party, resolveResult.ToUuid, cancellationToken);

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpDelete("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveAgent(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "to")][Required] Guid to,
        [FromQuery(Name = "cascade")] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        var problem = await clientDelegationService.RemoveAgent(party, to, cascade, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    [HttpGet("agents/accesspackages")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.ClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDelegatedAccessPackagesToAgentsViaPartyAsync(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "to")][Required] Guid to,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetDelegatedAccessPackagesToAgentsViaPartyAsync(party, to, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("clients/accesspackages")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.AgentDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDelegatedAccessPackagesFromClientsViaParty(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "from")][Required] Guid from,
        CancellationToken cancellationToken = default
    )
    {
        var result = await clientDelegationService.GetDelegatedAccessPackagesFromClientsViaParty(party, from, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpPost("agents/accesspackages")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [ProducesResponseType<List<DelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddAgentAccessPackageAsync(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "from")][Required] Guid from,
        [FromQuery(Name = "to")][Required] Guid to,
        [FromBody][Required] DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.AddDelegationForAgentAsync(party, from, to, payload, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpDelete("agents/accesspackages")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAgentAccessPackageAsync(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "from")][Required] Guid from,
        [FromQuery(Name = "to")][Required] Guid to,
        [FromBody][Required] DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default
    )
    {
        var result = await clientDelegationService.RemoveAgentDelegation(party, from, to, payload, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return NoContent();
    }
}
