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
using Altinn.AccessMgmt.PersistenceEF.Migrations;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using System.Net.Mime;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

[ApiController]
[Route("accessmanagement/api/v1/enduser/request")]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class RequestController(
    IRequestService requestService,
    IAssignmentService assignmentService,
    IDelegationService delegationService,
    IConnectionService connectionService,
    ConnectionQuery connectionQuery,
    IResourceService resourceService,
    IPackageService packageService,
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
    public async Task<IActionResult> GetSentRequests([FromQuery] Guid party, [FromQuery] Guid? to, [FromQuery, FromHeader] PagingInput paging, CancellationToken ct = default)
    {
        var validStatus = new List<RequestStatus>()
        {
            RequestStatus.Draft,
            RequestStatus.Pending,
            RequestStatus.Approved,
            RequestStatus.Rejected,
            RequestStatus.Withdrawn
        };

        var afterTime = DateTimeOffset.UtcNow.AddDays(-30);

        var result = await requestService.GetRequests(fromId: party, toId: to, status: validStatus, after: afterTime, ct);

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    [HttpGet("received")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<RequestDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReceivedRequests([FromQuery] Guid party, [FromQuery] Guid? from, [FromQuery, FromHeader] PagingInput paging, CancellationToken ct = default)
    {
        var validStatus = new List<RequestStatus>()
        {
            RequestStatus.Draft,
            RequestStatus.Pending,
            RequestStatus.Approved,
            RequestStatus.Rejected,
            RequestStatus.Withdrawn
        };

        var afterTime = DateTimeOffset.UtcNow.AddDays(-30);

        var result = await requestService.GetRequests(fromId: from, toId: party, status: validStatus, after: afterTime, ct);
        return result.IsSuccess ? Ok(PaginatedResult.Create(result.Value, null)) : result.Problem.ToActionResult();
    }

    /// <summary>
    /// Create request on behalf of a party
    /// </summary>
    [HttpPost]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRequest([FromQuery] Guid party, [FromQuery] Guid to, [FromBody]CreateRequestInput input, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        /*
        Person1 vil be FirmaA om en rettighet, derfor er to = FirmaA i queryparam. 
        Men da blir Assignment.From = FirmaA og Assignment.To = Party (Person1)
        GLHF
        */

        var authUserUuid = AuthenticationHelper.GetPartyUuid(HttpContext);
        var connections = await connectionQuery.HasConnection(to, authUserUuid);

        if (!connections.Result)
        {
            return Forbid();
        }

        var resource = input.Resource is { } ? await resourceService.GetResource(input.Resource, ct) : null;
        var package = input.Package is { } ? await packageService.GetPackage(input.Package, ct) : null;
        var role = RoleConstants.Rightholder;
        var status = RequestStatus.Pending;

        var result = await requestService.CreateRequest(
                new CreateRequestDto()
                {
                    From = to, // YES, this is correct
                    To = party,
                    Role = role.Id,
                    Status = status,
                    Resource = resource?.Id,
                    Package = package?.Id,
                },
                ct
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
    public async Task<IActionResult> ConfirmRequest([FromQuery] Guid party, [FromQuery] Guid id, CancellationToken ct = default)
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
    public async Task<IActionResult> WithdrawRequest([FromQuery] Guid party, [FromQuery] Guid id, CancellationToken ct = default)
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
    public async Task<IActionResult> RejectRequest([FromQuery] Guid party, [FromQuery] Guid id, CancellationToken ct = default)
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
    public async Task<IActionResult> ApproveRequest([FromQuery] Guid party, [FromQuery] Guid id, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var existingResult = await requestService.GetRequest(id, ct);
        if (existingResult.IsProblem)
        {
            return BadRequest(existingResult.Problem.ToProblemDetails());
        }

        var existing = existingResult.Value;

        if (existing is null || existing.Connection.From.Id != party)
        {
            return NotFound();
        }

        var authUserUuid = AuthenticationHelper.GetPartyUuid(HttpContext);

        return existing.Type switch
        {
            "resource" => await ApproveResourceRequest(party, authUserUuid, existing, ct),
            "package" => await ApprovePackageRequest(party, existing, ct),
            _ => BadRequest(),
        };
    }

    private async Task<IActionResult> ApprovePackageRequest(Guid partyUuid, RequestDto request, CancellationToken ct)
    {
        ValidationErrorBuilder errorBuilder = default;

        var assignment = await assignmentService.GetOrCreateAssignment(request.Connection.From.Id, request.Connection.To.Id, RoleConstants.Rightholder, cancellationToken: ct);
        if (assignment is null)
        {
            errorBuilder.Add(ValidationErrors.RequestFailedToApprove, "Approve", [new("Approve", $"Unable to get or create rightholder assignment")]);
            errorBuilder.TryBuild(out var problem);
            return problem.ToActionResult();
        }

        var result = await connectionService.AddPackage(
            request.Connection.From.Id,
            request.Connection.To.Id,
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

    private async Task<IActionResult> ApproveResourceRequest(Guid partyUuid, Guid authUserId, RequestDto request, CancellationToken ct)
    {
        var party = await entityService.GetEntity(partyUuid, ct); // valg avgiver

        var delegationCheck = await connectionService.ResourceDelegationCheck(
            authenticatedUserUuid: authUserId,
            party: partyUuid,
            resource: request.Resource.Urn,
            configureConnection: ConfigureConnections,
            cancellationToken: ct);

        if (delegationCheck.IsProblem)
        {
            return delegationCheck.Problem.ToActionResult();
        }

        var rightKeys = delegationCheck.Value.Rights
            .Where(r => r.Result)
            .Select(r => r.Right.Key)
            .ToList();

        if (!rightKeys.Any())
        {
            return Forbid("Missing rights to give");
        }

        var from = await entityService.GetEntity(request.Connection.From.Id, ct);
        var to = await entityService.GetEntity(request.Connection.To.Id, ct);
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
