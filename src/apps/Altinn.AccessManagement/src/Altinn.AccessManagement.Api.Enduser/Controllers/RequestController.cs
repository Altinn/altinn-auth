using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

[ApiController]
[Route("accessmanagement/api/v1/enduser/request")]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class RequestController(
    IRequestService requestService,
    IAssignmentService assignmentService,
    IConnectionService connectionService,
    ConnectionQuery connectionQuery,
    IResourceService resourceService,
    IEntityService entityService
    ) : ControllerBase
{
    private Action<ConnectionOptions> ConfigureConnections { get; } = options =>
    {
        options.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedWriteToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedReadFromEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedReadToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.FilterFromEntityTypes = [];
        options.FilterToEntityTypes = [];
    };

    [HttpGet("sent")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<RequestDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSentRequests(
        [FromQuery][Required] Guid party,
        [FromQuery] Guid? to,
        [FromQuery] RequestStatus[]? status,
        [FromQuery] string type,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken ct = default
        )
    {
        var statusFilter = status == null || !status.Any()
            ? new List<RequestStatus>() { RequestStatus.Draft, RequestStatus.Pending, RequestStatus.Approved, RequestStatus.Rejected, RequestStatus.Withdrawn }
            : status.ToList();

        var result = await requestService.GetSentRequests(party, to, statusFilter, type, ct);

        return result.IsSuccess ? Ok(PaginatedResult.Create(result.Value, null)) : result.Problem.ToActionResult();
    }

    [HttpGet("received")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<RequestDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReceivedRequests(
        [FromQuery][Required] Guid party,
        [FromQuery] Guid? from,
        [FromQuery] RequestStatus[]? status,
        [FromQuery] string type,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken ct = default
        )
    {
        var statusFilter = status == null || !status.Any()
            ? new List<RequestStatus>() { RequestStatus.Draft, RequestStatus.Pending, RequestStatus.Approved, RequestStatus.Rejected, RequestStatus.Withdrawn }
            : status.ToList();

        var result = await requestService.GetReceivedRequests(party, from, statusFilter, type, ct);
        return result.IsSuccess ? Ok(PaginatedResult.Create(result.Value, null)) : result.Problem.ToActionResult();
    }

    [HttpGet]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid id,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken ct = default
        )
    {
        var result = await requestService.GetRequest(id, ct);
        if (result.Value.From.Id != party && result.Value.To.Id != party)
        {
            return Forbid();
        }

        return result.IsSuccess ? Ok(result.Value) : result.Problem.ToActionResult();
    }

    /// <summary>
    /// Create request on behalf of a party
    /// </summary>
    [HttpPost("resource")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
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
        ValidationErrorBuilder errorBuilder = default;

        var authUserUuid = AuthenticationHelper.GetPartyUuid(HttpContext);
        var (hasConnections, _) = await connectionQuery.HasConnection(to, authUserUuid);
        if (!hasConnections)
        {
            errorBuilder.Add(ValidationErrors.RequestConnectionNotFound, "$QUERY/to", [new("to", $"No connection between party:'{party}' and to:'{to}'")]);
        }

        var resourceObj = await resourceService.GetResource(resource, ct);
        if (resourceObj is not { })
        {
            errorBuilder.Add(ValidationErrors.ResourceNotExists, "$QUERY/resource", [new("resource", $"Unable to get resource '{resource}'")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        /*
        Per (authUserUuid) ber om tilgang for Kari (party) til App (resource) hos Org (to).
        ==
        Per (by) ber om tilgang for Kari (for) til App (resource) hos Org (at).
        */
        var result = await requestService.CreateResourceRequest(
            toId: to, 
            fromId: party, 
            byId: authUserUuid, 
            roleId: RoleConstants.Rightholder.Id,
            resourceId: resourceObj.Id, 
            status: RequestStatus.Pending, 
            ct: ct
            );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create request on behalf of a party
    /// </summary>
    [HttpPost("package")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePackageRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid to,
        [FromQuery][Required] string package,
        CancellationToken ct = default
        )
    {
        ValidationErrorBuilder errorBuilder = default;

        var authUserUuid = AuthenticationHelper.GetPartyUuid(HttpContext);
        var (hasConnections, _) = await connectionQuery.HasConnection(to, authUserUuid);
        if (!hasConnections)
        {
            errorBuilder.Add(ValidationErrors.RequestConnectionNotFound, "$QUERY/to", [new("to", $"No connection exists between the authenticated user and party '{to}'.")]);
        }

        if (!PackageConstants.TryGetByAll(package, out var packageObj))
        {
            errorBuilder.Add(ValidationErrors.PackageNotExists, "$QUERY/package", [new("package", $"No package was found with value '{package}'.")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        /*
        A Request from Kari by NAV to BakerAS for AppResource01.
        Will create an Assignment from BakerAS to Kari with an AssignmentResource for AppResource01.
        */
        var result = await requestService.CreatePackageRequest(
           toId: to,
           fromId: party,
           byId: authUserUuid,
           roleId: RoleConstants.Rightholder.Id,
           packageId: packageObj.Id,
           status: RequestStatus.Pending,
           ct: ct
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Confirm a draft request (transitions Draft → Pending)
    /// </summary>
    [HttpPut("sent/confirm")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid id,
        CancellationToken ct = default
        )
    {
        return await UpdateRequestStatus(party, id, RequestStatus.Pending, ct);
    }

    /// <summary>
    /// Withdraw a request
    /// </summary>
    [HttpPut("sent/withdraw")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid id,
        CancellationToken ct = default
        )
    {
        return await UpdateRequestStatus(party, id, RequestStatus.Withdrawn, ct);
    }

    /// <summary>
    /// Reject a pending request
    /// </summary>
    [HttpPut("received/reject")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid id,
        CancellationToken ct = default
        )
    {
        return await UpdateRequestStatus(party, id, RequestStatus.Rejected, ct);
    }

    /// <summary>
    /// Approve a pending request — runs the same delegation logic as AddPackages/AddResourceRights
    /// </summary>
    [HttpPut("received/approve")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRequest(
        [FromQuery][Required] Guid party,
        [FromQuery][Required] Guid id,
        [FromBody] string[]? rightKeys,
        CancellationToken ct = default
        )
    {
        var requestResult = await requestService.GetRequest(id, ct);
        if (requestResult.IsProblem)
        {
            return BadRequest(requestResult.Problem.ToProblemDetails());
        }

        var request = requestResult.Value;
        if (request is null || request.From.Id != party)
        {
            return NotFound();
        }

        var authUserUuid = AuthenticationHelper.GetPartyUuid(HttpContext);

        return request.Type switch
        {
            "resource" => await ApproveResourceRequest(party, authUserUuid, request, rightKeys, ct),
            "package" => await ApprovePackageRequest(party, request, ct),
            _ => BadRequest(),
        };
    }

    private async Task<IActionResult> ApprovePackageRequest(Guid partyUuid, RequestDto request, CancellationToken ct)
    {
        ValidationErrorBuilder errorBuilder = default;

        var assignment = await assignmentService.GetOrCreateAssignment(request.From.Id, request.To.Id, RoleConstants.Rightholder, cancellationToken: ct);
        if (assignment is null)
        {
            errorBuilder.Add(ValidationErrors.RequestFailedToApprove, "Approve", [new("Approve", $"Unable to get or create rightholder assignment")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        var result = await connectionService.AddPackage(
            request.From.Id,
            request.To.Id,
            request.Package.Id.Value,
            ConfigureConnections,
            ct);

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        var updateResult = await requestService.UpdateRequest(partyUuid, request.Id, RequestStatus.Approved, ct);
        if (updateResult.IsProblem)
        {
            return updateResult.Problem.ToActionResult();
        }

        return Ok(request);
    }

    private async Task<IActionResult> ApproveResourceRequest(Guid partyUuid, Guid authUserId, RequestDto request, IEnumerable<string> rightKeys, CancellationToken ct)
    {
        var party = await entityService.GetEntity(partyUuid, ct); // valg avgiver

        var delegationCheck = await connectionService.ResourceDelegationCheck(
            authenticatedUserUuid: authUserId,
            party: partyUuid,
            resource: request.Resource.ReferenceId,
            configureConnection: ConfigureConnections,
            cancellationToken: ct);

        if (delegationCheck.IsProblem)
        {
            return delegationCheck.Problem.ToActionResult();
        }

        rightKeys = delegationCheck.Value.Rights
            .Where(r => r.Result)
            .Select(r => r.Right.Key)
            .ToList();

        if (!rightKeys.Any())
        {
            return Forbid("Missing rights to give");
        }

        var from = await entityService.GetEntity(request.From.Id, ct);
        var to = await entityService.GetEntity(request.To.Id, ct);
        var resource = await resourceService.GetResource(request.Resource.Id.Value, ct);

        var assignment = await assignmentService.GetOrCreateAssignment(from.Id, to.Id, RoleConstants.Rightholder, cancellationToken: ct);
        if (assignment is null)
        {
            return Problem("Unable to get or create rightholder assignment");
        }

        var result = await connectionService.AddResource(
            from,
            to,
            resource,
            new RightKeyListDto { DirectRightKeys = rightKeys },
            party,
            ConfigureConnections,
            ct);

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        var updateResult = await requestService.UpdateRequest(partyUuid, request.Id, RequestStatus.Approved, ct);
        if (updateResult.IsProblem)
        {
            return updateResult.Problem.ToActionResult();
        }

        return Ok(request);
    }

    private async Task<IActionResult> UpdateRequestStatus(Guid partyUuid, Guid id, RequestStatus status, CancellationToken ct)
    {
        var result = await requestService.UpdateRequest(partyUuid, id, status, ct);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }
}
