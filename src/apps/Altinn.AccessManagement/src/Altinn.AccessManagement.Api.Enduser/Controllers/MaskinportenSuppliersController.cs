using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for managing Maskinporten supplier assignments and scope delegations
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/maskinportensuppliers")]
public class MaskinportenSuppliersController(
    IMaskinportenSupplierService maskinportenSupplierService
    ) : ControllerBase
{
    /// <summary>
    /// Adds a supplier assignment to allow an organization to receive Maskinporten scope delegations
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddSupplier(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "supplier")] string supplier,
        CancellationToken cancellationToken = default)
    {
        // Look up supplier entity by organization number
        var supplierEntity = await maskinportenSupplierService.GetEntity(supplier, cancellationToken);
        if (supplierEntity.IsProblem)
        {
            return supplierEntity.Problem.ToActionResult();
        }

        var result = await maskinportenSupplierService.AddSupplier(party, supplierEntity.Value.Id, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all suppliers for the authenticated party
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_READ)]
    [ProducesResponseType<IEnumerable<ConnectionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSuppliers(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "supplier")] string? supplier = null,
        CancellationToken cancellationToken = default)
    {
        Guid? supplierEntityId = null;

        if (!string.IsNullOrWhiteSpace(supplier))
        {
            var supplierEntity = await maskinportenSupplierService.GetEntity(supplier, cancellationToken);
            if (supplierEntity.IsProblem)
            {
                return supplierEntity.Problem.ToActionResult();
            }

            supplierEntityId = supplierEntity.Value.Id;
        }

        var result = await maskinportenSupplierService.GetSuppliers(party, supplierEntityId, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Removes a supplier assignment
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveSupplier(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "supplier")] string supplier,
        [FromQuery(Name = "cascade")] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        var supplierEntity = await maskinportenSupplierService.GetEntity(supplier, cancellationToken);
        if (supplierEntity.IsProblem)
        {
            return supplierEntity.Problem.ToActionResult();
        }

        var problem = await maskinportenSupplierService.RemoveSupplier(party, supplierEntity.Value.Id, cascade, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    #region Resources

    /// <summary>
    /// Performs a delegation check for a MaskinportenSchema resource
    /// </summary>
    [HttpGet("resources/delegationcheck")]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_READ)]
    [ProducesResponseType<ResourceCheckDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DelegationCheck(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "resource")] string resource,
        CancellationToken cancellationToken = default)
    {
        Guid authenticatedUserUuid = AuthenticationHelper.GetAuthenticatedPartyUuid(HttpContext);
        string languageCode = this.GetLanguageCode();

        var result = await maskinportenSupplierService.ResourceDelegationCheck(authenticatedUserUuid, party, resource, languageCode: languageCode, cancellationToken: cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delegates a MaskinportenSchema resource to a supplier
    /// </summary>
    [HttpPost("resources")]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddResource(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "supplier")] string supplier,
        [Required][FromQuery(Name = "resource")] string resource,
        CancellationToken cancellationToken = default)
    {
        var supplierEntity = await maskinportenSupplierService.GetEntity(supplier, cancellationToken);
        if (supplierEntity.IsProblem)
        {
            return supplierEntity.Problem.ToActionResult();
        }

        var result = await maskinportenSupplierService.AddResource(party, supplierEntity.Value.Id, resource, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets delegated MaskinportenSchema resources for suppliers
    /// </summary>
    [HttpGet("resources")]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_READ)]
    [ProducesResponseType<IEnumerable<ResourcePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResources(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "supplier")] string? supplier = null,
        [FromQuery(Name = "resource")] string? resource = null,
        CancellationToken cancellationToken = default)
    {
        Guid? supplierEntityId = null;
        Guid? resourceId = null;

        if (!string.IsNullOrWhiteSpace(supplier))
        {
            var supplierEntity = await maskinportenSupplierService.GetEntity(supplier, cancellationToken);
            if (supplierEntity.IsProblem)
            {
                return supplierEntity.Problem.ToActionResult();
            }

            supplierEntityId = supplierEntity.Value.Id;
        }

        if (!string.IsNullOrWhiteSpace(resource))
        {
            var resourceEntity = await maskinportenSupplierService.GetResourceByRefId(resource, cancellationToken);
            if (resourceEntity.IsProblem)
            {
                return resourceEntity.Problem.ToActionResult();
            }

            resourceId = resourceEntity.Value.Id;
        }

        var result = await maskinportenSupplierService.GetSupplierResources(party, supplierEntityId, resourceId, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Removes a MaskinportenSchema resource delegation from a supplier
    /// </summary>
    [HttpDelete("resources")]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveResource(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "supplier")] string supplier,
        [Required][FromQuery(Name = "resource")] string resource,
        CancellationToken cancellationToken = default)
    {
        var supplierEntity = await maskinportenSupplierService.GetEntity(supplier, cancellationToken);
        if (supplierEntity.IsProblem)
        {
            return supplierEntity.Problem.ToActionResult();
        }

        var problem = await maskinportenSupplierService.RemoveResource(party, supplierEntity.Value.Id, resource, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    #endregion
}
