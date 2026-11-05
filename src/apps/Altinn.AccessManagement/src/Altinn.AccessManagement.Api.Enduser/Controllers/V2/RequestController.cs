using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessManagement.Core.Clients.Interfaces;
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
    IResourceRegistryClient resourceRegistryClient,
    IPDP pdp
    ) : RequestControllerBase<RequestDtoV2, RequestStatusV2>(
    requestService,
    assignmentService,
    connectionService,
    connectionQuery,
    resourceService,
    entityService,
    resourceRegistryClient,
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
    /// Create request for a resource on behalf of a party, optionally naming the rights being requested.
    /// Each right key must exist in the policy of the resource and be delegable.
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
}
