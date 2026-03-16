using System.Net.Mime;
using Altinn.AccessManagement.Api.ServiceOwner.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.ServiceOwner.Controllers;

/// <summary>
/// Request access
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/serviceowner/delegationrequests")]
public class RequestController(
    IRequestService requestService,
    IEntityService entityService,
    IResourceService resourceService,
    IPackageService packageService) : ControllerBase
{
    /// <summary>
    /// Get valid urn prefixes for party identification
    /// </summary>
    [HttpGet("_meta/urns/party")]
    [FeatureGate(RequirementType.Any, AccessMgmtFeatureFlags.EnableRequestAssignmentResource, AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [ProducesResponseType(typeof(IReadOnlyCollection<string>), StatusCodes.Status200OK)]
    public IActionResult GetValidUrns()
    {
        return Ok(ValidUrns);
    }

    /// <summary>
    /// Get resource requests for a given party
    /// </summary>
    [HttpGet("{id}/status")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [Authorize(Policy = AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_READ)]
    [AuditServiceOwnerConsumer]
    [ProducesResponseType<RequestStatus>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRequestStatus([FromRoute] Guid id, CancellationToken ct = default)
    {
        var result = await requestService.GetRequest(id, ct);
        return result.IsSuccess ? Ok(result.Value.Status) : result.Problem.ToActionResult();
    }

    /// <summary>
    /// Create a resource request for a given party and resource
    /// </summary>
    [HttpPost]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [Authorize(Policy = AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_WRITE)]
    [AuditServiceOwnerConsumer]
    [ProducesResponseType<RequestDto>(StatusCodes.Status202Accepted, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateServiceOwnerRequest input, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var fromInput = input.From;
        var toInput = input.To;

        var fromResult = await GetEntity(fromInput, "BODY/connection.from", ct);
        if (fromResult.IsProblem)
        {
            return fromResult.Problem.ToActionResult();
        }

        var toResult = await GetEntity(toInput, "BODY/connection.to", ct);
        if (toResult.IsProblem) 
        { 
            return toResult.Problem.ToActionResult(); 
        }

        var from = fromResult.Value;
        var to = toResult.Value;

        var role = RoleConstants.Rightholder;
        var status = RequestStatus.Draft;
        var resource = input.Resource is { } && input.Resource.HasValue() ? await resourceService.GetResource(input.Resource, ct) : null;
        var package = input.Package is { } && input.Package.HasValue() ? await packageService.GetPackage(input.Package, ct) : null;

        if (input.Resource is { } && input.Resource.HasValue() && resource == null)
        {
            errorBuilder.Add(ValidationErrorDescriptors.RequestedResourceNotFound, $"BODY/resource", [new("resource", $"Urn {input.Resource.ReferenceId} is not valid")]);
        }

        if (input.Package is { } && input.Package.HasValue() && package == null)
        {
            errorBuilder.Add(ValidationErrorDescriptors.RequestedPackageNotFound, $"BODY/package", [new("package", $"Urn {input.Package.ReferenceId} is not valid")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        var result = await requestService.CreateRequest(
            new CreateRequestDto()
            {
                From = from.Id,
                To = to.Id,
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

        result.Value.Links = BuildLinks(result.Value.Id);

        return Accepted(result.Value);
    }

    private async Task<Result<Entity>> GetEntity(string urn, string paramName, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        if (!ValidUrn(urn))
        {
            errorBuilder.Add(ValidationErrorDescriptors.InvalidUrn, paramName, [new(paramName, $"Urn {urn} is not valid")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem;
        }

        var urnSegments = urn.Split(":");
        var urnSuffix = urnSegments.Last();
        var key = urn[..(urn.Length - urnSuffix.Length - 1)];

        if (!ValidUrns.Contains(key))
        {
            errorBuilder.Add(ValidationErrorDescriptors.InvalidUrn, $"$QUERY/{paramName}", [new(paramName, $"Urn {urn} is not valid")]);
        }

        var entity = key switch
        {
            "urn:altinn:person:identifier-no" => await entityService.GetByPersNo(urnSuffix, ct),
            "urn:altinn:organization:identifier-no" => await entityService.GetByOrgNo(urnSuffix, ct),
            "urn:altinn:systemuser:uuid" or "urn:altinn:party:uuid" => await entityService.GetEntity(Guid.Parse(urnSuffix), ct),
            _ => null,
        };

        if (entity == null)
        {
            errorBuilder.Add(ValidationErrorDescriptors.NotFound, $"$QUERY/{paramName}", [new(paramName, $"Entity not found with matcing urn '{urn}'")]);
        }

        if (errorBuilder.TryBuild(out var problem1))
        {
            return problem1;
        }

        return entity;
    }
   
    private static bool ValidUrn(string urn) => ValidUrns.Any(t => urn.StartsWith(t));

    private static string[] ValidUrns => ["urn:altinn:person:identifier-no", "urn:altinn:organization:identifier-no"];

    private static RequestLinks BuildLinks(Guid requestId) => new()
    {
        DetailsLink = $"accessmanagement/api/v1/enduser/request/{requestId}/accept",
        StatusLink = $"accessmanagement/api/v1/serviceowner/delegationrequests/{requestId}"
    };
}
