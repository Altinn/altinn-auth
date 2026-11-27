using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.PEP.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using RequestDtoV2 = Altinn.Authorization.Api.Contracts.AccessManagement.V2.RequestDto;

namespace Altinn.AccessManagement.Api.Enduser.Controllers.V2;

/// <summary>
/// Version 2 of <see cref="Controllers.RequestController"/>.
///
/// NOTE! New functionality should be added here, and not in the base controller,
/// as it would result in it also being available in v1, which is not desired.
/// </summary>
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/request")]
[FeatureGate(AccessMgmtFeatureFlags.EnableEnduserRequestV2Api)]
public class RequestController(
    IRequestService requestService,
    IAssignmentService assignmentService,
    IConnectionService connectionService,
    ConnectionQuery connectionQuery,
    IResourceService resourceService,
    IEntityService entityService,
    IPDP pdp
    ) : RequestControllerBase<RequestDtoV2, RequestStatusV2>(
    requestService,
    assignmentService,
    connectionService,
    connectionQuery,
    resourceService,
    entityService,
    pdp
    )
{
    /// <inheritdoc/>
    protected override RequestDtoV2 ConvertRequest(AccessRequest request) => DtoMapper.ConvertToV2(request);

    /// <summary>
    /// Filters on every status, including those added after version 1.
    /// </summary>
    protected override IEnumerable<RequestStatusV2> GetStatusFilter(RequestStatusV2[]? status)
        => status == null || status.Length == 0 ? DefaultStatusFilter : status;

    /// <summary>
    /// Approve a pending request, optionally selecting the rights (right keys) to give for a resource request.
    /// The selection may include rights that were not requested. Each right key must exist in the policy of the
    /// resource and be delegable by the authenticated user on behalf of the party.
    /// Without a selection, what was requested is given. The request becomes Approved when what is given matches
    /// what was requested, and ApprovedWithAdjustments otherwise. For a request for the whole resource, selecting every right the
    /// authenticated user can delegate gives Approved, and selecting only some of them gives ApprovedWithAdjustments.
    /// </summary>
    /// <param name="party">The party approving, which must be the recipient of the request</param>
    /// <param name="id">The request to approve</param>
    /// <param name="input">The rights to give. Leave out, or leave the list empty, to give what was requested.</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPut("received/approve")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_REQUESTS_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDtoV2>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestDtoV2>> ApproveRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApproveRequestDto? input,
        CancellationToken ct = default
        )
    {
        return await ApproveRequestCore(party, id, input?.RightKeys?.ToList(), ct);
    }

    /// <summary>
    /// Create request for a resource on behalf of a party, optionally naming the rights being requested.
    /// Each right key must exist in the policy of the resource and be delegable.
    /// Sending the same request again returns the pending request. If a pending request for the resource asks for
    /// other rights, the response is 409, and that request must be withdrawn before a new one is sent.
    /// </summary>
    [HttpPost("resource")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_REQUESTS_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDtoV2>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> CreateResourceRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid to,
        [FromQuery][Required] string resource,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RequestedRightKeysDto? input,
        CancellationToken ct = default
        )
    {
        var authUserUuid = AuthenticationHelper.GetAuthenticatedPartyUuid(HttpContext);
        var (resourceObj, problem) = await ValidateResourceRequest(party, to, resource, ct);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        var rightKeysProblem = await RequestService.ValidateResourceRightKeys(resourceObj.RefId, input?.RequestedRightKeys, ct);
        if (rightKeysProblem is { })
        {
            return rightKeysProblem.ToActionResult();
        }

        var result = await RequestService.CreateResourceRequest(
            toId: to,
            fromId: party,
            byId: authUserUuid,
            roleId: RoleConstants.Rightholder.Id,
            resourceId: resourceObj.Id,
            requestedRightKeys: input?.RequestedRightKeys,
            status: RequestStatusV2.Pending,
            ct: ct
            );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(ConvertRequest(result.Value));
    }

    /// <summary>
    /// Change the rights (right keys) asked for in a pending resource request, replacing the ones it asked for before.
    /// Only the party that sent the request can change it, and only while it is pending. Each right key must exist in the
    /// policy of the resource and be delegable, and at least one is required: to ask for the whole resource instead, withdraw
    /// the request and send a new one. The rights are stored in the order given.
    /// The recipient is not notified again.
    /// </summary>
    /// <param name="party">The party that sent the request</param>
    /// <param name="id">The request to change</param>
    /// <param name="input">The rights to ask for instead</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPut("sent/resource")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_REQUESTS_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDtoV2>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RequestDtoV2>> UpdateResourceRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RequestedRightKeysDto? input,
        CancellationToken ct = default
        )
    {
        // A missing body is accepted here so it gets the same 400 as a body without right keys, instead of 415.
        var result = await RequestService.UpdateResourceRequestRightKeys(party, id, input?.RequestedRightKeys, ct);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(ConvertRequest(result.Value));
    }
}
