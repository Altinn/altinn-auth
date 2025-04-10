using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Mappers;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Extensions;
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
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class AccessPartiesController(IHttpContextAccessor accessor, IAssignmentService assignmentService, IMapper<AssignmentExternal, Assignment> mapper) : ControllerBase
{
    private IHttpContextAccessor Accessor { get; } = accessor;

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
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ServiceFilter(typeof(AuthorizePartyUuidClaimFilter))]
    [ProducesResponseType<AssignmentExternal>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PostAccessParty([FromQuery] Guid party, [FromQuery] Guid to, CancellationToken cancellationToken = default)
    {
        var partyUuid = Accessor.GetPartyUuid();
        var audit = new ChangeRequestOptions()
        {
            ChangedBy = partyUuid,
            ChangedBySystem = AuditDefaults.EnduserApi
        };

        var assignment = await AssignmentService.GetOrCreateAssignment(party, to, "rightholder", audit, cancellationToken);

        if (assignment.IsProblem)
        {
            return assignment.Problem.ToActionResult();
        }

        return Ok(Mapper.Map(assignment.Value));
    }

    /// <summary>
    /// Creates an assignment between the authenticated user's selected party and the specified target party.
    /// </summary>
    /// <param name="party">The GUID identifying the party the authenticated user is acting on behalf of.</param>
    /// <param name="to">The GUID identifying the target party to which the assignment should be created.</param>
    /// <param name="cascade">Must be set to true if enduser should delete active foreign connections.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    [HttpDelete]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ServiceFilter(typeof(AuthorizePartyUuidClaimFilter))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAccessParty([FromQuery] Guid party, [FromQuery] Guid to, [FromQuery] bool cascade = false, CancellationToken cancellationToken = default)
    {
        var partyUuid = Accessor.GetPartyUuid();
        var audit = new ChangeRequestOptions()
        {
            ChangedBy = partyUuid,
            ChangedBySystem = AuditDefaults.EnduserApi
        };

        var problem = await AssignmentService.DeleteAssignment(party, to, "rightholder", audit, cascade, cancellationToken);

        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }
}
