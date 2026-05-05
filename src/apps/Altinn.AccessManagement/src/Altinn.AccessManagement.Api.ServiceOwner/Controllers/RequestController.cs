using System.Net.Mime;
using Altinn.AccessManagement.Api.ServiceOwner.Validation;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    IAuditAccessor auditAccessor,
    IResourceRegistryClient resourceRegistryClient,
    IOptions<GeneralSettings> generalSettings
    ) : ControllerBase
{
    private readonly GeneralSettings _generalSettings = generalSettings.Value;

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
    [HttpPost("resource")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentResource)]
    [Authorize(Policy = AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_WRITE)]
    [AuditServiceOwnerConsumer]
    [ProducesResponseType<RequestDto>(StatusCodes.Status202Accepted, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateResourceRequest([FromBody] RequestResourceDto data, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var fromResult = await GetEntity(data.From, "/from", ct);
        fromResult.Problems(ref errorBuilder);

        var toResult = await GetEntity(data.To, "/to", ct);
        toResult.Problems(ref errorBuilder);

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        /*
        NAV (authUserUuid) ber om tilgang for Kari (party) til App (resource) hos Org (to).
        ==
        NAV (by) ber om tilgang for Kari (for) til App (resource) hos Org (at).
        */

        return await CreateResourceRequest(
            toId: toResult.Entity.Id,
            fromId: fromResult.Entity.Id,
            byId: auditAccessor.AuditValues.ChangedBy,
            roleId: RoleConstants.Rightholder.Id,
            status: RequestStatus.Draft,
            resourceRef: new RequestReferenceDto() { ReferenceId = data.Resource },
            ct: ct
            );
    }

    /// <summary>
    /// Create a package request
    /// </summary>
    [HttpPost("package")]
    [FeatureGate(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage)]
    [Authorize(Policy = AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_WRITE)]
    [AuditServiceOwnerConsumer]
    [ProducesResponseType<RequestDto>(StatusCodes.Status202Accepted, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePackageRequest([FromBody] RequestPackageDto data, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        if (string.IsNullOrEmpty(data.Package))
        {
            errorBuilder.Add(ValidationErrorDescriptors.InvalidUrn, "/package", [new("package", "Package must be defined.")]);
        }

        var fromResult = await GetEntity(data.From, "/from", ct);
        fromResult.Problems(ref errorBuilder);

        var toResult = await GetEntity(data.To, "/to", ct);
        toResult.Problems(ref errorBuilder);

        if (!PackageConstants.TryGetByAll(data.Package, out var packageObj))
        {
            errorBuilder.Add(ValidationErrors.PackageNotExists, "/package", [new("package", $"No package was found with value '{data.Package}'.")]);
        }

        if (packageObj != null && !packageObj.Entity.IsAssignable)
        {
            errorBuilder.Add(ValidationErrors.PackageIsNotAssignable, "/package", [new("package", $"Package '{data.Package}' is not assignable.")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        /*
        /*
        NAV (authUserUuid) ber om tilgang for Kari (party) til App (resource) hos Org (to).
        ==
        NAV (by) ber om tilgang for Kari (for) til App (resource) hos Org (at).
        */
        return await CreatePackageRequest(
            toId: toResult.Entity.Id,
            fromId: fromResult.Entity.Id,
            byId: auditAccessor.AuditValues.ChangedBy,
            roleId: RoleConstants.Rightholder.Id,
            status: RequestStatus.Draft,
            package: new RequestReferenceDto() { Id = packageObj.Id, ReferenceId = packageObj.Entity.Urn },
            ct: ct
            );
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
        var resourceParamName = "resource";
        var packageParamName = "package";

        if (input?.Resource is null && input?.Package is null)
        {
            errorBuilder.Add(ValidationErrors.RequestMissingResourceOrPackage, resourceParamName, [new(resourceParamName, "Either package or resource must be defined.")]);
            errorBuilder.Add(ValidationErrors.RequestMissingResourceOrPackage, packageParamName, [new(packageParamName, "Either package or resource must be defined.")]);
        }

        if (input?.Resource?.HasValue() == true && input?.Package?.HasValue() == true)
        {
            errorBuilder.Add(ValidationErrors.ResourceAndPackageIsSpecified, resourceParamName, [new(resourceParamName, "Both package and resource are specified. Only one must be defined.")]);
            errorBuilder.Add(ValidationErrors.ResourceAndPackageIsSpecified, packageParamName, [new(packageParamName, "Both package and resource are specified. Only one must be defined.")]);
        }

        var fromResult = await GetEntity(input?.From, "connection/from", ct);
        fromResult.Problems(ref errorBuilder);

        var toResult = await GetEntity(input?.To, "connection/to", ct);
        toResult.Problems(ref errorBuilder);

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        /*
        NAV (authUserUuid) ber om tilgang for Kari (party) til App (resource) hos Org (to).
        ==
        NAV (by) ber om tilgang for Kari (for) til App (resource) hos Org (at).
        */

        if (input?.Resource is { } && input.Resource.HasValue())
        {
            return await CreateResourceRequest(
                toId: fromResult.Entity.Id,
                fromId: toResult.Entity.Id,
                byId: auditAccessor.AuditValues.ChangedBy,
                roleId: RoleConstants.Rightholder.Id,
                status: RequestStatus.Draft,
                resourceRef: input.Resource,
                ct: ct
            );
        }

        if (input?.Package is { } && input.Package.HasValue())
        {
            return await CreatePackageRequest(
                toId: fromResult.Entity.Id,
                fromId: toResult.Entity.Id,
                byId: auditAccessor.AuditValues.ChangedBy,
                roleId: RoleConstants.Rightholder.Id,
                status: RequestStatus.Draft,
                package: input.Package,
                ct: ct
            );
        }

        errorBuilder.Add(ValidationErrorDescriptors.RequestedResourceNotFound);
        errorBuilder.Add(ValidationErrorDescriptors.RequestedPackageNotFound);
        errorBuilder.TryToActionResult(out var problemDetails);
        return problemDetails;
    }

    private async Task<IActionResult> CreateResourceRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, RequestStatus status, RequestReferenceDto resourceRef, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;
        var paramName = "resource";

        var resource = await resourceService.GetResource(resourceRef, ct);
        if (resource is null)
        {
            errorBuilder.Add(ValidationErrorDescriptors.RequestedResourceNotFound, paramName, [new(paramName, $"Resource with reference ID '{resourceRef.ReferenceId}' was not found.")]);
        }

        try
        {
            var serviceResource = await resourceRegistryClient.GetResource(resourceRef.ReferenceId, ct);

            if (!serviceResource.Delegable)
            {
                errorBuilder.Add(ValidationErrors.ResourceIsNotDelegable, paramName, [new(paramName, $"Resource with reference ID '{resourceRef.ReferenceId}' is not delegable.")]);
            }
        }
        catch
        {
            // errorBuilder.Add(ValidationErrorDescriptors.RequestedResourceNotFound, paramName, [new(paramName, $"Resource with reference ID '{resourceRef.ReferenceId}' was not found.")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        // Fetch provider claim from token
        var providerClaim = User.FindFirst(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute)?.Value;

        var byEntity = await entityService.GetEntity(byId, ct);
        if (resource.Provider.RefId != byEntity.OrganizationIdentifier
            && !string.Equals(resource.Provider.Code, providerClaim, StringComparison.OrdinalIgnoreCase))
        {
            errorBuilder.Add(ValidationErrorDescriptors.RequestedResourceNotByServiceOwner, paramName, [new(paramName, $"Resource with reference ID '{resourceRef.ReferenceId}' is not owned by serviceowner.")]);
        }

        if (errorBuilder.TryBuild(out problem))
        {
            return problem.ToActionResult();
        }

        var result = await requestService.CreateResourceRequest(
            toId: toId,
            fromId: fromId,
            byId: byId,
            roleId: roleId,
            resourceId: resource.Id,
            status: status,
            ct: ct
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        result.Value.Links = BuildLinks(result.Value.Id);

        return Accepted(result.Value);
    }

    private async Task<IActionResult> CreatePackageRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, RequestStatus status, RequestReferenceDto package, CancellationToken ct = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        if (!PackageConstants.TryGetByAll(package.ReferenceId, out var packageObj))
        {
            errorBuilder.Add(ValidationErrors.PackageNotExists, "/package", [new("package", $"No package was found with value '{package.ReferenceId}'.")]);
        }

        if (!packageObj.Entity.IsAssignable)
        {
            errorBuilder.Add(ValidationErrors.PackageIsNotAssignable, "/package", [new("package", $"Package with reference ID '{package.ReferenceId}' is not assignable.")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem.ToActionResult();
        }

        var result = await requestService.CreatePackageRequest(
           toId: toId,
           fromId: fromId,
           byId: byId,
           roleId: roleId,
           packageId: packageObj.Id,
           status: status,
           ct: ct
       );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        result.Value.Links = BuildLinks(result.Value.Id);

        return Accepted(result.Value);
    }

    private delegate void ValidationRule(ref ValidationErrorBuilder errors);

    private async Task<(Entity Entity, ValidationRule Problems)> GetEntity(string urn, string paramName, CancellationToken ct = default)
    {
        var accumulatedErrors = new List<ValidationRule>();
        ValidationRule errorBuilderFunc = (ref ValidationErrorBuilder errors) =>
        {
            foreach (var error in accumulatedErrors)
            {
                error(ref errors);
            }
        };

        if (!ValidUrn(urn))
        {
            accumulatedErrors.Add((ref ValidationErrorBuilder errorBuilder) => errorBuilder.Add(ValidationErrorDescriptors.InvalidUrn, paramName, [new(paramName, $"Urn {urn} is not valid")]));
            return (null, errorBuilderFunc);
        }

        var urnSegments = urn.Split(":");
        var urnSuffix = urnSegments.Last();
        var key = urn[..(urn.Length - urnSuffix.Length - 1)];

        if (!ValidUrns.Contains(key))
        {
            accumulatedErrors.Add((ref ValidationErrorBuilder errorBuilder) => errorBuilder.Add(ValidationErrorDescriptors.InvalidUrn, $"/{paramName}", [new(paramName, $"Urn {urn} is not valid")]));
            return (null, errorBuilderFunc);
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
            accumulatedErrors.Add((ref ValidationErrorBuilder errorBuilder) => errorBuilder.Add(ValidationErrorDescriptors.NotFound, $"/{paramName}", [new(paramName, $"Entity not found with matcing urn '{urn}'")]));
        }

        return (entity, errorBuilderFunc);
    }

    private static bool ValidUrn(string urn) => ValidUrns.Any(t => urn.StartsWith(t));

    private static string[] ValidUrns => ["urn:altinn:person:identifier-no", "urn:altinn:organization:identifier-no"];

    private RequestLinks BuildLinks(Guid requestId) => new()
    {
        DetailsLink = $"https://am.ui.{_generalSettings.Hostname}/accessmanagement/ui/requests/resource?requestId={requestId}",
        StatusLink = $"https://platform.{_generalSettings.Hostname}/accessmanagement/api/v1/serviceowner/delegationrequests/{requestId}/status"
    };
}
