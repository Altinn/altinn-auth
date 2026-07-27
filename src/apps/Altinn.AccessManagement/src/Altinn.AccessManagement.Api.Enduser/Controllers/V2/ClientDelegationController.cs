using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractsV2 = Altinn.Authorization.Api.Contracts.AccessManagement.V2;

namespace Altinn.AccessManagement.Api.Enduser.Controllers.V2;

[ApiController]
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations")]
[Tags("Client Delegation")]
public class ClientDelegationController(
    IHttpContextAccessor httpContextAccessor,
    IInputValidation inputValidation,
    IClientDelegationService clientDelegationService) : ControllerBase
{
    #region My

    [HttpGet("my/clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_READ)]
    [ProducesResponseType<PaginatedResult<ContractsV2.MyClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyClients(
        [FromQuery(Name = "provider")] List<Guid>? provider,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var authenticatedUser = AuthenticationHelper.GetAuthenticatedPartyUuid(httpContextAccessor.HttpContext);
        if (authenticatedUser == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await clientDelegationService.GetMyClientsV2(authenticatedUser, provider, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("my/clientproviders")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_READ)]
    [ProducesResponseType<PaginatedResult<ContractsV2.MyClientProviderDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyClientProviders(CancellationToken cancellationToken = default)
    {
        var authenticatedUser = AuthenticationHelper.GetAuthenticatedPartyUuid(httpContextAccessor.HttpContext);
        if (authenticatedUser == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await clientDelegationService.GetMyProvidersV2(authenticatedUser, cancellationToken);
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
        var authenticatedUser = AuthenticationHelper.GetAuthenticatedPartyUuid(httpContextAccessor.HttpContext);
        if (authenticatedUser == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await clientDelegationService.DeleteMyProvider(authenticatedUser, provider, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return NoContent();
    }

    [HttpDelete("my/clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMyClientViaProvider(
        [FromQuery(Name = "provider")][Required] Guid provider,
        [FromQuery(Name = "client")][Required] Guid client,
        [FromQuery(Name = "cascade")] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        var authenticatedUser = AuthenticationHelper.GetAuthenticatedPartyUuid(httpContextAccessor.HttpContext);
        if (authenticatedUser == Guid.Empty)
        {
            return Unauthorized();
        }

        var problem = await clientDelegationService.RemoveClientFromAgent(provider, client, authenticatedUser, cascade, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    [HttpPost("my/clients/accesspackages/delete")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<DelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMyPackagesToClientViaProvider(
        [FromQuery(Name = "provider")][Required] Guid provider,
        [FromQuery(Name = "client")][Required] Guid client,
        [FromBody][Required] DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        var authenticatedUser = AuthenticationHelper.GetAuthenticatedPartyUuid(httpContextAccessor.HttpContext);
        if (authenticatedUser == Guid.Empty)
        {
            return Unauthorized();
        }

        // The agent is the authenticated user, so agent errors point at the provider
        // parameter that names the relationship.
        var result = await clientDelegationService.DeleteMyClient(
            authenticatedUser,
            provider,
            client,
            payload,
            new ClientDelegationParameterNames(Party: "provider", Client: "client", Agent: "provider"),
            cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpPost("my/clients/resources/delete")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<ContractsV2.ResourceDelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMyResourcesToClientViaProvider(
        [FromQuery(Name = "provider")][Required] Guid provider,
        [FromQuery(Name = "client")][Required] Guid client,
        [FromBody][Required] ContractsV2.ResourceDelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        var authenticatedUser = AuthenticationHelper.GetAuthenticatedPartyUuid(httpContextAccessor.HttpContext);
        if (authenticatedUser == Guid.Empty)
        {
            return Unauthorized();
        }

        // The agent is the authenticated user, so agent errors point at the provider
        // parameter that names the relationship.
        var result = await clientDelegationService.DeleteMyClientResources(
            authenticatedUser,
            provider,
            client,
            payload,
            new ClientDelegationParameterNames(Party: "provider", Client: "client", Agent: "provider"),
            cancellationToken);
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
    [ProducesResponseType<PaginatedResult<ContractsV2.ClientDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClients(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "roles")] List<string>? roles,
        [FromQuery(Name = "packages")] List<string>? packages,
        [FromQuery(Name = "resources")] List<string>? resources,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetClientsV2(party, roles, packages, resources, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("agents")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<ContractsV2.AgentDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAgents(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetAgentsV2(party, cancellationToken);
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
        [FromQuery(Name = "agent")] Guid? agent,
        [FromBody] PersonInput? person,
        CancellationToken cancellationToken = default)
    {
        var entity = await inputValidation.SanitizeToInput(
            party,
            agent,
            person,
            options =>
            {
                options.AllowedToEntityTypes = [EntityTypeConstants.Person, EntityTypeConstants.SystemUser];
                options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
                options.ToParameterName = "agent";
            },
            cancellationToken);

        if (entity.IsProblem)
        {
            return entity.Problem.ToActionResult();
        }

        var result = await clientDelegationService.AddAgent(party, entity.Value.Id, ClientDelegationParameterNames.V2, cancellationToken);
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
        [FromQuery(Name = "agent")][Required] Guid agent,
        [FromQuery(Name = "cascade")] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        var problem = await clientDelegationService.RemoveAgent(party, agent, cascade, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    [HttpDelete("agents/clients")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveAgentsClient(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "client")][Required] Guid client,
        [FromQuery(Name = "agent")][Required] Guid agent,
        [FromQuery(Name = "cascade")] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        var problem = await clientDelegationService.RemoveClientFromAgent(party, client, agent, cascade, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    [HttpGet("agents/accesspackages")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<ContractsV2.ClientPackagesDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDelegatedAccessPackagesToAgentsViaPartyAsync(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "agent")][Required] Guid agent,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetDelegatedAccessPackagesToAgentsViaPartyV2(party, agent, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("clients/accesspackages")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<ContractsV2.AgentPackagesDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDelegatedAccessPackagesFromClientsViaParty(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "client")][Required] Guid client,
        CancellationToken cancellationToken = default
    )
    {
        var result = await clientDelegationService.GetDelegatedAccessPackagesFromClientsViaPartyV2(party, client, cancellationToken);
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
        [FromQuery(Name = "client")][Required] Guid client,
        [FromQuery(Name = "agent")][Required] Guid agent,
        [FromBody][Required] DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.DelegateAccessPackageToAgent(party, client, agent, payload, ClientDelegationParameterNames.V2, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpPost("agents/accesspackages/delete")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<DelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAgentAccessPackage(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "client")][Required] Guid client,
        [FromQuery(Name = "agent")][Required] Guid agent,
        [FromBody][Required] DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default
    )
    {
        var result = await clientDelegationService.RemoveAgentDelegation(party, client, agent, payload, ClientDelegationParameterNames.V2, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpGet("agents/resources")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<ContractsV2.ClientResourcesDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDelegatedResourcesToAgentsViaParty(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "agent")][Required] Guid agent,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.GetDelegatedResourcesToAgentsViaPartyV2(party, agent, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("clients/resources")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    [ProducesResponseType<PaginatedResult<ContractsV2.AgentResourcesDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDelegatedResourcesFromClientsViaParty(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "client")][Required] Guid client,
        CancellationToken cancellationToken = default
    )
    {
        var result = await clientDelegationService.GetDelegatedResourcesFromClientsViaPartyV2(party, client, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpPost("agents/resources")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<ContractsV2.ResourceDelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DelegateResourceToAgent(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "client")][Required] Guid client,
        [FromQuery(Name = "agent")][Required] Guid agent,
        [FromBody][Required] ContractsV2.ResourceDelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        var result = await clientDelegationService.DelegateResourceToAgent(party, client, agent, payload, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    [HttpPost("agents/resources/delete")]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<List<ContractsV2.ResourceDelegationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAgentResource(
        [FromQuery(Name = "party")][Required] Guid party,
        [FromQuery(Name = "client")][Required] Guid client,
        [FromQuery(Name = "agent")][Required] Guid agent,
        [FromBody][Required] ContractsV2.ResourceDelegationBatchInputDto payload,
        CancellationToken cancellationToken = default
    )
    {
        var result = await clientDelegationService.RemoveAgentResourceDelegation(party, client, agent, payload, ClientDelegationParameterNames.V2, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    #endregion
}
