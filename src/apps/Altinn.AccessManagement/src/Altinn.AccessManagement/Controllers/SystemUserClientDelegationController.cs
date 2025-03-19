using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using MassTransit.Initializers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Controllers;

/// <summary>
/// Controller for internal api operations for system user client delegation.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/internal/[controller]")]
[ApiExplorerSettings(IgnoreApi = false)]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class SystemUserClientDelegationController : ControllerBase
{
    private readonly IConnectionRepository connectionRepository;
    private readonly IDelegationService delegationService;
    private readonly IDelegationRepository delegationRepository;
    private readonly IAssignmentRepository assignmentRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemUserClientDelegationController"/> class.
    /// </summary>
    public SystemUserClientDelegationController(
        IConnectionRepository connectionRepository, 
        IDelegationService delegationService, 
        IDelegationRepository delegationRepository,
        IAssignmentRepository assignmentRepository
        )
    {
        this.connectionRepository = connectionRepository;
        this.delegationService = delegationService;
        this.delegationRepository = delegationRepository;
        this.assignmentRepository = assignmentRepository;
    }

    /// <summary>
    /// Post client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="request">Request Dto</param>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> PostClientDelegation([FromQuery] Guid party, [FromBody] CreateSystemDelegationRequestDto request)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var delegations = await delegationService.CreateClientDelegation(request, userId, party);
        var result = new List<ExtDelegation>();
        
        foreach (var delegation in delegations)
        {
            result.Add(await delegationRepository.GetExtended(delegation.Id));
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="client">The client the authenticated user is removing access from</param>
    /// <param name="systemUser">The system user the authenticated user is removing client access to</param>
    [HttpDelete]
    [Route("delete")]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> DeleteClientDelegations([FromQuery] Guid party, [FromQuery] Guid client, [FromQuery] Guid systemUser)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        /*
        Find all delegations with facilitator(party) and From(client) and To(systemUser)
        */

        // Using connection to lookup to bypass assignment lookup.
        var filter = connectionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, client);
        filter.Equal(t => t.FacilitatorId, party);
        filter.Equal(t => t.ToId, systemUser);
        var delegationConnections = await connectionRepository.Get();

        foreach (var delegationId in delegationConnections.Select(t => t.Id))
        {
            await delegationRepository.Delete(delegationId);
        }

        return Ok();
    }

    /// <summary>
    /// Delete client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="delegationId">The delegation identifier</param>
    [HttpDelete]
    [Route("deletedelegation")]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> DeleteClientDelegation([FromQuery] Guid party, [FromQuery] Guid delegationId)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        /*
        if party is facilitator for delegation
        */

        var delegation = await delegationRepository.Get(delegationId);
        if (delegation == null)
        {
            return BadRequest("Delegation not found");
        }

        if (delegation.FacilitatorId != party)
        {
            return BadRequest("Party does not match delegation facilitator");
        }

        await delegationRepository.Delete(delegation.Id);

        return Ok();
    }

    /// <summary>
    /// Delete client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="assignmentId">The assignment identifier</param>
    [HttpDelete]
    [Route("deleteassignment")]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> DeleteClientAssignment([FromQuery] Guid party, [FromQuery] Guid assignmentId)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var assignment = await assignmentRepository.Get(assignmentId);
        if (assignment == null)
        {
            return BadRequest("Assignment not found");
        }

        if (assignment.FromId.Equals(party))
        {
            return BadRequest("Assignment not connected to party");
        }

        await assignmentRepository.Delete(assignment.Id);

        return Ok();
    }

    /// <summary>
    /// Gets all client delegations for a given system user
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="systemUser">The system user the authenticated user is delegating access to</param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    public async Task<ActionResult> GetClientDelegations([FromQuery] Guid party, [FromQuery] Guid systemUser)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var filter = delegationRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, systemUser);
        filter.Equal(t => t.FacilitatorId, party);
        var res = await delegationRepository.GetExtended(filter);

        return Ok(res);
    }
}
