using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
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
    private readonly IRoleRepository roleRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemUserClientDelegationController"/> class.
    /// </summary>
    public SystemUserClientDelegationController(
        IConnectionRepository connectionRepository, 
        IDelegationService delegationService, 
        IDelegationRepository delegationRepository,
        IAssignmentRepository assignmentRepository,
        IRoleRepository roleRepository
        )
    {
        this.connectionRepository = connectionRepository;
        this.delegationService = delegationService;
        this.delegationRepository = delegationRepository;
        this.assignmentRepository = assignmentRepository;
        this.roleRepository = roleRepository;
    }

    /// <summary>
    /// Gets all client delegations for a given system user
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="systemUser">The system user the authenticated user is delegating access to</param>
    /// <returns><seealso cref="ConnectionDto"/>List of connections</returns>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    public async Task<ActionResult<ConnectionDto>> GetClientDelegations([FromQuery] Guid party, [FromQuery] Guid systemUser)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var f = connectionRepository.CreateFilterBuilder();
        f.Equal(t => t.ToId, systemUser);
        f.Equal(t => t.FacilitatorId, party);
        var res = new List<ConnectionDto>();

        foreach (var r in await connectionRepository.GetExtended(f))
        {
            res.Add(ConnectionConverter.ConvertToDto(r));
        }
       
        return Ok(res);
    }

    /// <summary>
    /// Post client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="request">Request Dto</param>
    /// <returns><seealso cref="CreateDelegationResponse"/>List of delegation responses</returns>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult<CreateDelegationResponse>> PostClientDelegation([FromQuery] Guid party, [FromBody] CreateSystemDelegationRequestDto request)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var options = new ChangeRequestOptions()
        {
            ChangedBy = userId,
            ChangedBySystem = AuditDefaults.EnduserApi
        };

        var delegations = await delegationService.CreateClientDelegation(request, party, options);

        var result = new List<CreateDelegationResponse>();
        foreach (var delegation in delegations)
        {
            result.Add(ConnectionConverter.ConvertToResponseModel(await connectionRepository.Get(delegation.Id)));
        }

        // Remark: Kan ikke garantere at det KUN er delegeringer som er opprettet i denne handlingen som blir returnert.
        return Ok(result);
    }

    /// <summary>
    /// Delete client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="delegationId">The delegation identifier</param>
    [HttpDelete]
    [Route("deletedelegation")]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> DeleteDelegation([FromQuery] Guid party, [FromQuery] Guid delegationId)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        /*
        - [X] Delegation exists
        - [X] if party is facilitator for delegation
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

        var from = await assignmentRepository.Get(delegation.FromId);
        var to = await assignmentRepository.Get(delegation.ToId);
        if (!from.ToId.Equals(party) || !to.FromId.Equals(party))
        {
            return BadRequest("Party does not match delegation assignments");
        }

        var options = new ChangeRequestOptions()
        {
            ChangedBy = userId,
            ChangedBySystem = AuditDefaults.EnduserApi
        };

        await delegationRepository.Delete(delegation.Id, options: options);

        return Ok();
    }

    /// <summary>
    /// Delete client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="assignmentId">The assignment identifier</param>
    /// <param name="cascade">If true; dependent rows in the database will be deleted</param>
    [HttpDelete]
    [Route("deleteassignment")]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> DeleteAssignment([FromQuery] Guid party, [FromQuery] Guid assignmentId, [FromQuery] bool cascade = false)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        /*
        - [X] Assignment exists
        - [X] Assignment connected to party
        - [X] Assignment role is owned by Digdir
        - [X] Assignment not connected to any Delegation (or cascade = true)
        - [X] Temp: Only 'agent' role - Get this from queryparam future
        */
 
        string roleIdentifier = "agent"; 

        var assignment = await assignmentRepository.GetExtended(assignmentId);
        if (assignment == null)
        {
            return BadRequest("Assignment not found");
        }

        if (!assignment.FromId.Equals(party))
        {
            return BadRequest("Assignment not from party");
        }

        if (!assignment.Role.Code.Equals(roleIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            return Problem($"You cannot removed assignments with this role '{assignment.Role.Code}', only '{roleIdentifier}'");
        }

        if (!cascade)
        {
            var delegationsFromAssignment = await delegationRepository.Get(t => t.FromId, assignment.Id);
            var delegationsToAssignment = await delegationRepository.Get(t => t.ToId, assignment.Id);

            if (delegationsFromAssignment.Any() || delegationsToAssignment.Any())
            {
                return Problem("Assignment is active in one or more delegations and cascadeflag is false.");
            }
        }

        var options = new ChangeRequestOptions()
        {
            ChangedBy = userId,
            ChangedBySystem = AuditDefaults.EnduserApi
        };

        await assignmentRepository.Delete(assignment.Id, options: options);

        return Ok();
    }

    /// <summary>
    /// Delete client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="agentId">The agent/system party identifier</param>
    /// <param name="cascade">If true; dependent rows in the database will be deleted</param>
    [HttpDelete]
    [Route("deleteagentassignment")]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> DeleteAgentAssignment([FromQuery] Guid party, [FromQuery] Guid agentId, [FromQuery] bool cascade = false)
    {
        var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        /*
        - [X] Assignment exists
        - [X] Assignment connected to party
        - [X] Assignment role is owned by Digdir
        - [X] Assignment not connected to any Delegation (or cascade = true)
        */

        string roleIdentifier = "agent";

        var role = (await roleRepository.Get(t => t.Code, roleIdentifier)).FirstOrDefault();
        if (role == null)
        {
            return Problem($"Unable to find role '{roleIdentifier}'");
        }

        if (!role.Code.Equals(roleIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            return Problem($"You cannot removed assignments with this role '{role.Code}', only '{roleIdentifier}'");
        }

        var assignmentFilter = assignmentRepository.CreateFilterBuilder();
        assignmentFilter.Equal(t => t.FromId, party);
        assignmentFilter.Equal(t => t.ToId, agentId);
        assignmentFilter.Equal(t => t.RoleId, role.Id);
        var assignments = await assignmentRepository.GetExtended(assignmentFilter);
        if (assignments == null || !assignments.Any())
        {
            return BadRequest("Assignment not found");
        }

        if (assignments.Count() > 1)
        {
            return BadRequest("To many assignment found");
        }

        var assignment = assignments.First();

        if (!cascade)
        {
            var delegationsFromAssignment = await delegationRepository.Get(t => t.FromId, assignment.Id);
            var delegationsToAssignment = await delegationRepository.Get(t => t.ToId, assignment.Id);

            if (delegationsFromAssignment.Any() || delegationsToAssignment.Any())
            {
                return Problem("Assignment is active in one or more delegations and cascadeflag is false.");
            }
        }

        var options = new ChangeRequestOptions()
        {
            ChangedBy = userId,
            ChangedBySystem = AuditDefaults.EnduserApi
        };

        await assignmentRepository.Delete(assignment.Id, options: options);

        return Ok();
    }
}
