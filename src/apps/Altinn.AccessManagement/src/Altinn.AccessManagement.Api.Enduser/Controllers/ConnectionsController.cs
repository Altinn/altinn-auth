using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for en user api operations for connections
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/connections")]
[FeatureGate(AccessManagementEnduserFeatureFlags.ControllerConnections)]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class ConnectionsController(IConnectionService connectionService) : ControllerBase
{
    private IConnectionService ConnectionService { get; } = connectionService;

    private Action<ConnectionOptions> ConfigureConnections { get; } = options =>
    {
        options.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organisation];
        options.AllowedWriteToEntityTypes = [EntityTypeConstants.Organisation];
        options.AllowedReadFromEntityTypes = [EntityTypeConstants.Organisation, EntityTypeConstants.Person];
        options.AllowedReadToEntityTypes = [EntityTypeConstants.Organisation, EntityTypeConstants.Person];
        options.FilterFromEntityTypes = [];
        options.FilterToEntityTypes = [];
    };

    /// <summary>
    /// Get connections between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [ProducesResponseType<PaginatedResult<ConnectionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConnections([FromQuery] ConnectionInput connection, [FromQuery, FromHeader] PagingInput paging, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserReadConnection(connection.Party, connection.From, connection.To) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        var validFromUuid = Guid.TryParse(connection.From, out var fromUuid);
        var validToUuid = Guid.TryParse(connection.To, out var toUuid);
        _ = Guid.TryParse(connection.Party, out var partyUuid);

        var result = await ConnectionService.Get(partyUuid, validFromUuid ? fromUuid : null, validToUuid ? toUuid : null, ConfigureConnections, cancellationToken);
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
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
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
        var result = await ConnectionService.AddAssignment(fromUuid, toUuid, ConfigureConnections, cancellationToken);
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
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
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
        problem = await ConnectionService.RemoveAssignment(fromUuid, toUuid, cascade, ConfigureConnections, cancellationToken);
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
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<PackagePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPackages([FromQuery] ConnectionInput connection, [FromQuery, FromHeader] PagingInput paging, CancellationToken cancellationToken = default)
    {
        if (EnduserValidationRules.EnduserReadConnection(connection.Party, connection.From, connection.To) is var problem && problem is { })
        {
            return problem.ToActionResult();
        }

        var validFromUuid = Guid.TryParse(connection.From, out var fromUuid);
        var validToUuid = Guid.TryParse(connection.To, out var toUuid);
        _ = Guid.TryParse(connection.Party, out var partyUuid);

        var result = await ConnectionService.GetPackages(partyUuid, validFromUuid ? fromUuid : null, validToUuid ? toUuid : null, ConfigureConnections, cancellationToken);
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
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<AssignmentPackageDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
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
        async Task<Result<AssignmentPackageDto>> AddPackage()
        {
            if (packageId.HasValue)
            {
                return await ConnectionService.AddPackage(fromUuid, toUuid, packageId.Value, ConfigureConnections, cancellationToken);
            }

            return await ConnectionService.AddPackage(fromUuid, toUuid, package, ConfigureConnections, cancellationToken);
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
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
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
                return await ConnectionService.RemovePackage(fromUuid, toUuid, packageId.Value, ConfigureConnections, cancellationToken);
            }

            return await ConnectionService.RemovePackage(fromUuid, toUuid, package, ConfigureConnections, cancellationToken);
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
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
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
                return await ConnectionService.CheckPackage(party, packages, packageIds, ConfigureConnections, cancellationToken);
            }

            return await ConnectionService.CheckPackage(party, packageIds, ConfigureConnections, cancellationToken);
        }

        var result = await CheckPackage();
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }
}
