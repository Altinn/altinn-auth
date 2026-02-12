using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
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
    IHttpContextAccessor httpContextAccessor,
    IInputValidation inputValidation,
    IClientDelegationService clientDelegationService) : ControllerBase
{
    #region My
 
    [HttpGet("my/clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_READ)]
    [ProducesResponseType<PaginatedResult<MyClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyClients(
        [FromQuery(Name = "provider")] List<Guid>? provider,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var useruuid = AuthenticationHelper.GetPartyUuid(httpContextAccessor.HttpContext);
        var result = await clientDelegationService.GetMyClients(useruuid, provider, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("my/clientproviders")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_READ)]
    [ProducesResponseType<PaginatedResult<AgentDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyClientProviders(CancellationToken cancellationToken = default)
    {
        var useruuid = AuthenticationHelper.GetPartyUuid(httpContextAccessor.HttpContext);
        var result = await clientDelegationService.GetMyProviders(useruuid, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpDelete("my/clientproviders")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMyAgentViaParty(
        [FromQuery(Name = "provider")][Required] Guid provider,
        CancellationToken cancellationToken = default)
    {
        var useruuid = AuthenticationHelper.GetPartyUuid(httpContextAccessor.HttpContext);
        var result = await clientDelegationService.DeleteMyProvider(useruuid, provider, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return NoContent();
    }

    [HttpDelete("my/clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<DelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMyClientViaParty(
        [FromQuery(Name = "provider")][Required] Guid provider,
        [FromQuery(Name = "from")][Required] Guid from,
        [FromBody][Required] DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        var useruuid = AuthenticationHelper.GetPartyUuid(httpContextAccessor.HttpContext);
        var result = await clientDelegationService.DeleteMyClient(useruuid, provider, from, payload, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    #endregion

    #region Provider

    [HttpGet("clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.ClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClients(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "roles")] List<string>? roles,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetClients(party, roles, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<Altinn.Authorization.Api.Contracts.AccessManagement.AgentDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAgents(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetAgents(party, cancellationToken);
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
        var entity = await inputValidation.SanitizeToInput(
            party,
            to,
            person,
            options =>
            {
                options.AllowedToEntityTypes = [EntityTypeConstants.Person, EntityTypeConstants.SystemUser];
                options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
            },
            cancellationToken);

        if (entity.IsProblem)
        {
            return entity.Problem.ToActionResult();
        }

        var result = await clientDelegationService.AddAgent(party, entity.Value.Id, cancellationToken);
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
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<DelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DelegateAccessPackageToAgent(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "from")][Required] Guid from,
        [FromQuery(Name = "to")][Required] Guid to,
        [FromBody][Required] DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.DelegateAccessPackageToAgent(party, from, to, payload, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpDelete("agents/accesspackages")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<DelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAgentAccessPackage(
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

        return Ok(result.Value);
    }

    #endregion
}
