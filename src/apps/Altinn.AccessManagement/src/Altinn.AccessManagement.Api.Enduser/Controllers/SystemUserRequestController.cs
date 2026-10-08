using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using ValidationErrors = Altinn.AccessMgmt.Core.Utils.Models.ValidationErrors;

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
        [FromQuery][Required] string organization,
        [FromQuery][Required] string package,
        CancellationToken ct = default
        )
    {
        if (!OrganizationNumber.TryParse(organization, null, out _))
        {
            ValidationErrorBuilder errors = default;
            errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/organization");
            errors.TryBuild(out var invalidOrganization);
            return invalidOrganization.ToActionResult();
        }

        string consumerOrgNo;
        try
        {
            consumerOrgNo = OrgUtil.GetAuthenticatedParty(User) is ConsentPartyUrn.OrganizationId consumer
                ? consumer.Value.ToString()
                : null;
        }
        catch (FormatException)
        {
            consumerOrgNo = null;
        }

        if (string.IsNullOrEmpty(consumerOrgNo))
        {
            return Problems.SystemUserRequestNotAllowed.ToActionResult();
        }

        var systemUserUuid = AuthenticationHelper.GetSystemUserUuid(HttpContext);

        var result = await requestService.CreateSystemUserPackageRequest(
           organizationNo: organization,
           systemUserId: systemUserUuid,
           consumerOrgNo: consumerOrgNo,
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
