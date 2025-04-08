using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Mappers;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Filters;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for end user api operations for parties which have provided access to the user or it's organizations.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/access/parties")]
[FeatureGate(AccessManagementEnduserFeatureFlags.ControllerAccessParties)]
public class AccessPartiesController(IAssignmentService assignmentService, IMapper<AssignmentExternal, Assignment> mapper) : ControllerBase
{
    private IAssignmentService AssignmentService { get; } = assignmentService;

    private IMapper<AssignmentExternal, Assignment> Mapper { get; } = mapper;

    /// <summary>
    /// Get access parties
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> GetAccessParties()
    {
        return await Task.FromResult(Ok());
    }

    /// <summary>
    /// Creates an assignment between the authenticated user's selected party and the specified target party.
    /// </summary>
    /// <param name="party">The GUID identifying the party the authenticated user is acting on behalf of.</param>
    /// <param name="to">The GUID identifying the target party to which the assignment should be created.</param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
    [ServiceFilter(typeof(AuthorizePartyUuidClaimFilter))]
    [ProducesResponseType<AssignmentExternal>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PostAccessParty([FromQuery] Guid party, [FromQuery] Guid to)
    {
        // When history is enabled pass partyUuid downstream to GetOrCreateAssignment
        //// var partyUuid = Accessor.GetPartyUuid();

        // TODO: Andreas - Verify
        var options = new ChangeRequestOptions()
        {
            ChangedBy = party,
            ChangedBySystem = AuditDefaults.EnduserApi
        };

        var assignment = await AssignmentService.GetOrCreateAssignment2(party, to, "rightholders", options);

        if (assignment.IsProblem)
        {
            return assignment.Problem.ToActionResult();
        }

        return Ok(Mapper.Map(assignment.Value));
    }
}
