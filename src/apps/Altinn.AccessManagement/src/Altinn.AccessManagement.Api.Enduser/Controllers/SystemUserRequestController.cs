using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for access requests where a system user is the requester.
/// Authorized by the dedicated Maskinporten scope for system user requests.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/systemuser/request")]
public class SystemUserRequestController(
    IRequestService requestService
    ) : ControllerBase
{
    /// <summary>
    /// Create a package request where a system user is the requester.
    /// Authorized by the dedicated Maskinporten scope for system user requests.
    /// </summary>
    [HttpPost("package")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableSystemUserRequests)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_SYSTEMUSER_REQUESTS_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePackageRequest(
        [FromQuery][Required] Guid to,
        [FromQuery][Required] string package,
        CancellationToken ct = default
        )
    {
        var systemUserUuid = AuthenticationHelper.GetSystemUserUuid(HttpContext);

        var result = await requestService.CreateSystemUserPackageRequest(
           toId: to,
           systemUserId: systemUserUuid,
           roleId: RoleConstants.Rightholder.Id,
           package: package,
           status: RequestStatus.Pending,
           ct: ct
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }
}
