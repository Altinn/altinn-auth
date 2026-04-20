using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for managing Maskinporten consumer connections (from supplier perspective)
/// </summary>
[ApiController]
[FeatureGate(AccessMgmtFeatureFlags.EnableEnduserMaskinportenAdminApi)]
[Route("accessmanagement/api/v1/enduser/maskinportenconsumers")]
public class MaskinportenConsumersController(
    IMaskinportenSupplierService maskinportenSupplierService
    ) : ControllerBase
{
    /// <summary>
    /// Gets all consumers for the authenticated supplier
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_READ)]
    [ProducesResponseType<IEnumerable<ConnectionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConsumers(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "consumer")] string? consumer = null,
        CancellationToken cancellationToken = default)
    {
        Guid? consumerEntityId = null;

        if (!string.IsNullOrWhiteSpace(consumer))
        {
            var consumerEntity = await maskinportenSupplierService.GetEntity(consumer, cancellationToken);
            if (consumerEntity.IsProblem)
            {
                return consumerEntity.Problem.ToActionResult();
            }

            consumerEntityId = consumerEntity.Value.Id;
        }

        var result = await maskinportenSupplierService.GetConsumers(party, consumerEntityId, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Removes a consumer connection (supplier relinquishes their access)
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveConsumer(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "consumer")] string consumer,
        [FromQuery(Name = "cascade")] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        var consumerEntity = await maskinportenSupplierService.GetEntity(consumer, cancellationToken);
        if (consumerEntity.IsProblem)
        {
            return consumerEntity.Problem.ToActionResult();
        }

        // Note: From supplier perspective, they are removing themselves (party) from consumer
        // So the fromId is consumer, toId is supplier (party)
        var problem = await maskinportenSupplierService.RemoveSupplier(consumerEntity.Value.Id, party, cascade, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    #region Resources

    /// <summary>
    /// Gets MaskinportenSchema resources delegated from consumers
    /// </summary>
    [HttpGet("resources")]
    [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_READ)]
    [ProducesResponseType<IEnumerable<ResourcePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResources(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "consumer")] string? consumer = null,
        [FromQuery(Name = "resource")] string? resource = null,
        CancellationToken cancellationToken = default)
    {
        Guid? consumerEntityId = null;
        Guid? resourceId = null;

        if (!string.IsNullOrWhiteSpace(consumer))
        {
            var consumerEntity = await maskinportenSupplierService.GetEntity(consumer, cancellationToken);
            if (consumerEntity.IsProblem)
            {
                return consumerEntity.Problem.ToActionResult();
            }

            consumerEntityId = consumerEntity.Value.Id;
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

        var result = await maskinportenSupplierService.GetConsumerResources(party, consumerEntityId, resourceId, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Removes a MaskinportenSchema resource delegation (supplier relinquishes access to a specific resource)
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
        [Required][FromQuery(Name = "consumer")] string consumer,
        [Required][FromQuery(Name = "resource")] string resource,
        CancellationToken cancellationToken = default)
    {
        var consumerEntity = await maskinportenSupplierService.GetEntity(consumer, cancellationToken);
        if (consumerEntity.IsProblem)
        {
            return consumerEntity.Problem.ToActionResult();
        }

        // From supplier perspective: consumer is fromId, supplier (party) is toId
        var problem = await maskinportenSupplierService.RemoveResource(consumerEntity.Value.Id, party, resource, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    #endregion
}
