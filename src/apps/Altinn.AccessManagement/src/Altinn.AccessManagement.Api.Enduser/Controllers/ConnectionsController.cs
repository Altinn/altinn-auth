using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Enduser.Services;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using CompactRelationDto = Altinn.AccessMgmt.Persistence.Services.Models.CompactRelationDto;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for en user api operations for connections
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/connections")]
[FeatureGate(AccessManagementEnduserFeatureFlags.ControllerConnections)]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class ConnectionsController(IEnduserConnectionService connectionService) : ControllerBase
{
    private IEnduserConnectionService ConnectionService { get; } = connectionService;

    /// <summary>
    /// Get connections between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [ProducesResponseType<PaginatedResult<CompactRelationDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConnections([FromQuery] ConnectionInput connection, [FromQuery, FromHeader] PagingInput paging, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserReadConnection(connection.Party, connection.From, connection.To) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        Guid.TryParse(connection.From, out var fromUuid);
        Guid.TryParse(connection.To, out var toUuid);
        var result = await ConnectionService.Get(fromUuid, toUuid, cancellationToken: cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    /// <summary>
    /// Add package to connection (assignment or delegation)
    /// </summary>
    [HttpPost]
    [DbAudit(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApiStr)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<Assignment>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddAssignment([FromQuery] ConnectionInput connection, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserAddConnection(connection.Party, connection.From, connection.To) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        Guid.TryParse(connection.From, out var fromUuid);
        Guid.TryParse(connection.To, out var toUuid);
        var result = await ConnectionService.AddAssignment(fromUuid, toUuid, "rettighetshaver", cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Remove package from connection (assignment or delegation)
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [DbAudit(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApiStr)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveAssignment([FromQuery] ConnectionInput connection, [FromQuery] bool cascade = false, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserRemoveConnection(connection.Party, connection.From, connection.To) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        Guid.TryParse(connection.From, out var fromUuid);
        Guid.TryParse(connection.To, out var toUuid);
        problem = await ConnectionService.RemoveAssignment(fromUuid, toUuid, "rettighetshaver", cascade, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>
    /// Creates an assignment between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet("accesspackages")]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [DbAudit(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApiStr)]
    [ProducesResponseType<PaginatedResult<PackagePermission>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPackages([FromQuery] ConnectionInput connection, [FromQuery, FromHeader] PagingInput paging, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserReadConnection(connection.Party, connection.From, connection.To) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        Guid.TryParse(connection.From, out var fromUuid);
        Guid.TryParse(connection.To, out var toUuid);
        var result = await ConnectionService.GetPackages(fromUuid, toUuid, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    /// <summary>
    /// Add package to connection (assignment or delegation)
    /// </summary>
    [HttpPost("accesspackages")]
    [DbAudit(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApiStr)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<AssignmentPackage>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddPackages([FromQuery] ConnectionInput connection, [FromQuery] Guid? packageId, [FromQuery] string package, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserAddConnectionPackage(connection.Party, connection.From, connection.To, packageId, package) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        Guid.TryParse(connection.From, out var fromUuid);
        Guid.TryParse(connection.To, out var toUuid);
        async Task<Result<AssignmentPackage>> AddPackage()
        {
            if (packageId.HasValue)
            {
                return await ConnectionService.AddPackage(fromUuid, toUuid, "rettighetshaver", packageId.Value, cancellationToken);
            }

            return await ConnectionService.AddPackage(fromUuid, toUuid, "rettighetshaver", package, cancellationToken);
        }

        var result = await AddPackage();
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Remove package from connection (assignment or delegation)
    /// </summary>
    [HttpDelete("accesspackages")]
    [DbAudit(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApiStr)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemovePackages([FromQuery] ConnectionInput connection, [FromQuery] Guid? packageId, [FromQuery] string package, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserRemoveConnectionPacakge(connection.Party, connection.From, connection.To, packageId, package) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        Guid.TryParse(connection.From, out var fromUuid);
        Guid.TryParse(connection.To, out var toUuid);
        async Task<ValidationProblemInstance> RemovePackage()
        {
            if (packageId.HasValue)
            {
                return await ConnectionService.RemovePackage(fromUuid, toUuid, "rettighetshaver", packageId.Value, cancellationToken);
            }

            return await ConnectionService.RemovePackage(fromUuid, toUuid, "rettighetshaver", package, cancellationToken);
        }

        problem = await RemovePackage();

        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>
    /// API for delegation check of access packages, for which packages the authenticated user has permission to assign to others on behalf of the specified party.
    /// </summary>
    [HttpGet("accesspackages/delegationcheck")]
    [DbAudit(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApiStr)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<PaginatedResult<AccessPackageDto.Check>>(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckPackage([FromQuery] Guid party, [FromQuery] IEnumerable<Guid>? packageIds, [FromQuery] IEnumerable<string>? packages, CancellationToken cancellationToken = default)
    {
        async Task<Result<IEnumerable<AccessPackageDto.Check>>> CheckPackage()
        {
            if (packages.Any())
            {
                return await ConnectionService.CheckPackage(party, packages, packageIds, cancellationToken);
            }

            return await ConnectionService.CheckPackage(party, packageIds, cancellationToken);
        }

        var result = await CheckPackage();
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }
}
