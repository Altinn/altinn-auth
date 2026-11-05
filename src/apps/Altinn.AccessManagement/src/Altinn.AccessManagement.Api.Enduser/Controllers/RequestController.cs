using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Utils;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.PEP.Helpers;
using Altinn.Common.PEP.Interfaces;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Access requests (tilgangsforespørsler) in the enduser API, version 1.
/// The shared actions are in <see cref="RequestControllerBase{TRequest, TStatus}"/>.
///
/// Deprecated: use version 2 at accessmanagement/api/v2/enduser/request. Responses carry the
/// <c>Deprecation</c> header and a <c>successor-version</c> link to version 2.
/// </summary>
[Route("accessmanagement/api/v1/enduser/request")]
[DeprecatedApi(RequestController.DeprecationDate, SuccessorVersion = "/accessmanagement/api/v2/enduser/request")]
public class RequestController(
    IRequestService requestService,
    IAssignmentService assignmentService,
    IConnectionService connectionService,
    ConnectionQuery connectionQuery,
    IResourceService resourceService,
    IEntityService entityService,
    IResourceRegistryClient resourceRegistryClient,
    IPDP pdp
    ) : RequestControllerBase<RequestDto, RequestStatus>(
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
    /// <summary>
    /// The date version 1 is deprecated, sent in the <c>Deprecation</c> header.
    /// </summary>
    public const string DeprecationDate = "2027-05-31";

    /// <inheritdoc/>
    protected override RequestDto ConvertRequest(AccessRequest request) => DtoMapper.ConvertToV1(request);

    /// <summary>
    /// Filters on the version 1 statuses. <see cref="RequestDto.Status"/> reports <see cref="RequestStatusV2.PartialApprove"/>
    /// as <see cref="RequestStatus.Approved"/>, so filtering on Approved also includes PartialApprove.
    /// </summary>
    protected override IEnumerable<RequestStatusV2> GetStatusFilter(RequestStatus[]? status)
        => status == null || status.Length == 0 ? DefaultStatusFilter : DtoMapper.ConvertFromV1StatusFilter(status);

    /// <summary>
    /// Create request on behalf of a party
    /// </summary>
    [HttpPost("resource")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_REQUESTS_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateResourceRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid to,
        [FromQuery][Required] string resource,
        [FromBody] string[]? rightKeys,
        CancellationToken ct = default
        )
    {
        var authUserUuid = AuthenticationHelper.GetAuthenticatedPartyUuid(HttpContext);
        var (resourceObj, problem) = await ValidateResourceRequest(party, to, resource, ct);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        /*
        Per (authUserUuid) ber om tilgang for Kari (party) til App (resource) hos Org (to).
        ==
        Per (by) ber om tilgang for Kari (for) til App (resource) hos Org (at).
        */

        // Guaranteed non-null here: ValidateResourceRequest returned no problem, so the GetResource lookup succeeded.
        var result = await RequestService.CreateResourceRequest(
            toId: to,
            fromId: party,
            byId: authUserUuid,
            roleId: RoleConstants.Rightholder.Id,
            resourceId: resourceObj!.Id,
            requestedRightKeys: null,
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
