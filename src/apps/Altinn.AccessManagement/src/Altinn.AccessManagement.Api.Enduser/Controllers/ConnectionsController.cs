using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Utils;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Validation;
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
/// Controller for enduser api operations for connections
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/connections")]
[FeatureGate(AccessManagementEnduserFeatureFlags.ControllerConnections)]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class ConnectionsController(
    IConnectionService ConnectionService,
    IUserProfileLookupService UserProfileLookupService,
    ISingleRightsService singleRightsService,
    IEntityService EntityService,
    IResourceService resourceService
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

    /// <summary>
    /// Get connections between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [ProducesResponseType<PaginatedResult<ConnectionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConnections(
        [FromQuery] ConnectionInput connection,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateReadConnection(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var partyUuid = Guid.Parse(connection.Party);
        var validFromUuid = Guid.TryParse(connection.From, out var fromUuid);
        var validToUuid = Guid.TryParse(connection.To, out var toUuid);

        var result = await ConnectionService.Get(partyUuid, validFromUuid ? fromUuid : null, validToUuid ? toUuid : null, ConfigureConnections, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    #region Assignment

    /// <summary>
    /// Add a new rightholder connection
    /// </summary>
    [HttpPost]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AddAssignment([FromQuery] ConnectionInput connection, [FromBody] PersonInput? person, CancellationToken cancellationToken = default)
    {
        bool hasPersonInputParameter = person is { };

        var validationErrors = hasPersonInputParameter
            ? ValidationComposer.Validate(
                ConnectionValidation.ValidateAddAssignmentWithPersonInput(connection.Party, connection.From, person.PersonIdentifier, person.LastName))
            : ValidationComposer.Validate(
                ConnectionValidation.ValidateAddAssignmentWithConnectionInput(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var fromUuid = Guid.Parse(connection.From);

        var resolver = new ToUuidResolver(EntityService, UserProfileLookupService);
        var resolveResult = hasPersonInputParameter
            ? await resolver.ResolveWithPersonInputAsync(person, HttpContext, cancellationToken)
            : await resolver.ResolveWithConnectionInputAsync(Guid.Parse(connection.To), false, cancellationToken);

        if (!resolveResult.Success)
        {
            return resolveResult.ErrorResult!;
        }

        var toUuid = resolveResult.ToUuid;

        var result = await ConnectionService.AddAssignment(fromUuid, toUuid, ConfigureConnections, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Remove an existing rightholder connection with option to cascade delete any assignments tied to the connection
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
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateRemoveConnection(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var fromUuid = Guid.Parse(connection.From);
        var toUuid = Guid.Parse(connection.To);

        var problem = await ConnectionService.RemoveAssignment(fromUuid, toUuid, cascade, ConfigureConnections, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    #endregion

    #region Packages

    /// <summary>
    /// Gets all access packages between the authenticated user's selected party and the specified target party.
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
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateReadConnection(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var partyUuid = Guid.Parse(connection.Party);
        var validFromUuid = Guid.TryParse(connection.From, out var fromUuid);
        var validToUuid = Guid.TryParse(connection.To, out var toUuid);

        var result = await ConnectionService.GetPackages(partyUuid, validFromUuid ? fromUuid : null, validToUuid ? toUuid : null, ConfigureConnections, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    /// <summary>
    /// Add package to an existing rightholder connection
    /// </summary>
    [HttpPost("accesspackages")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<AssignmentPackageDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddPackages([FromQuery] ConnectionInput connection, [FromBody] PersonInput? person, [FromQuery] Guid? packageId, [FromQuery] string package, CancellationToken cancellationToken = default)
    {
        bool hasPersonInputParameter = person is { };

        var validationErrors = hasPersonInputParameter
            ? ValidationComposer.Validate(
                ConnectionValidation.ValidateAddPackageToConnectionWithPersonInput(connection.Party, connection.From, person.PersonIdentifier, person.LastName, packageId, package))
            : ValidationComposer.Validate(
                ConnectionValidation.ValidateAddPackageToConnectionWithConnectionInput(connection.Party, connection.From, connection.To, packageId, package));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var fromUuid = Guid.Parse(connection.From);

        var resolver = new ToUuidResolver(EntityService, UserProfileLookupService);
        var resolveResult = hasPersonInputParameter
            ? await resolver.ResolveWithPersonInputAsync(person, HttpContext, cancellationToken)
            : await resolver.ResolveWithConnectionInputAsync(Guid.Parse(connection.To), true, cancellationToken);

        if (!resolveResult.Success)
        {
            return resolveResult.ErrorResult!;
        }

        var toUuid = resolveResult.ToUuid;

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
    /// Remove package from rightholder connection
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
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateRemovePackageFromConnection(connection.Party, connection.From, connection.To, packageId, package));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var fromUuid = Guid.Parse(connection.From);
        var toUuid = Guid.Parse(connection.To);

        async Task<ValidationProblemInstance> RemovePackage()
        {
            if (packageId.HasValue)
            {
                return await ConnectionService.RemovePackage(fromUuid, toUuid, packageId.Value, ConfigureConnections, cancellationToken);
            }

            return await ConnectionService.RemovePackage(fromUuid, toUuid, package, ConfigureConnections, cancellationToken);
        }

        var problem = await RemovePackage();

        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>
    /// Delegation check of access packages, for which packages the authenticated user has permission to assign to others on behalf of the specified party.
    /// </summary>
    [HttpGet("accesspackages/delegationcheck")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<PaginatedResult<AccessPackageDto.AccessPackageDtoCheck>>(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckPackage([FromQuery] Guid party, [FromQuery] IEnumerable<Guid>? packageIds, [FromQuery] IEnumerable<string>? packages, CancellationToken cancellationToken = default)
    {
        async Task<Result<IEnumerable<AccessPackageDto.AccessPackageDtoCheck>>> CheckPackage()
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

    #endregion

    #region Roles

    /// <summary>
    /// Gets all roles between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<RolePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles([FromQuery] ConnectionInput connection, [FromQuery, FromHeader] PagingInput paging, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateReadConnection(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var partyUuid = Guid.Parse(connection.Party);
        var validFromUuid = Guid.TryParse(connection.From, out var fromUuid);
        var validToUuid = Guid.TryParse(connection.To, out var toUuid);

        var result = await ConnectionService.GetRoles(partyUuid, validFromUuid ? fromUuid : null, validToUuid ? toUuid : null, ConfigureConnections, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    /// <summary>
    /// Remove Altinn 2 role from connection
    /// </summary>
    [HttpDelete("roles")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveRole([FromQuery] ConnectionInput connection, [FromQuery] string roleCode, CancellationToken cancellationToken = default)
    {
        Guid.TryParse(connection.From, out var fromUuid);
        Guid.TryParse(connection.To, out var toUuid);

        async Task<ValidationProblemInstance> RemoveRole()
        {
            return await Task.FromResult<ValidationProblemInstance>(null); // ToDo: Implement when role service is ready
        }

        var problem = await RemoveRole();

        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    #endregion

    #region Resources

    /// <summary>
    /// Gets all resources between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet("resources")]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<ResourcePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResources([FromQuery] ConnectionInput connection, [FromQuery, FromHeader] PagingInput paging, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateReadConnection(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var partyUuid = Guid.Parse(connection.Party);
        var validFromUuid = Guid.TryParse(connection.From, out var fromUuid);
        var validToUuid = Guid.TryParse(connection.To, out var toUuid);

        // Does not return Actions => Use DelegationCheck
        var result = await ConnectionService.GetResources(partyUuid, validFromUuid ? fromUuid : null, validToUuid ? toUuid : null, ConfigureConnections, cancellationToken);
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    /// <summary>
    /// Add resource to an existing rightholder connection
    /// </summary>
    [HttpPost("resources")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<AssignmentResourceDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddResource([FromQuery] ConnectionInput connection, [FromQuery] string resource, [FromBody] string[] actionKeys, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(ConnectionValidation.ValidateAddResourceToConnectionWithConnectionInput(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var byId = AuthenticationHelper.GetPartyUuid(this.HttpContext);

        if (!Guid.TryParse(connection.From, out var fromId) || !Guid.TryParse(connection.To, out var toId) || byId == Guid.Empty)
        {
            return Problem();
        }

        var from = await EntityService.GetEntity(fromId, cancellationToken);
        var to = await EntityService.GetEntity(toId, cancellationToken);
        var by = await EntityService.GetEntity(byId, cancellationToken);
        var resourceObj = await resourceService.GetResource(resource, cancellationToken);

        var result = await singleRightsService.TryWriteDelegationPolicyRules(from, to, resourceObj, actionKeys.ToList(), by, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add resource to an existing rightholder connection
    /// </summary>
    [HttpPut("resources")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<AssignmentResourceDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateResource([FromQuery] ConnectionInput connection, [FromQuery] string resource, [FromBody] string[] actionKeys, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(ConnectionValidation.ValidateAddResourceToConnectionWithConnectionInput(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var byId = AuthenticationHelper.GetPartyUuid(this.HttpContext);

        if (!Guid.TryParse(connection.From, out var fromId) || !Guid.TryParse(connection.To, out var toId) || byId == Guid.Empty)
        {
            return Problem();
        }

        var from = await EntityService.GetEntity(fromId, cancellationToken);
        var to = await EntityService.GetEntity(toId, cancellationToken);
        var by = await EntityService.GetEntity(byId, cancellationToken);
        var resourceObj = await resourceService.GetResource(resource, cancellationToken);

        var result = await singleRightsService.TryWriteDelegationPolicyRules(from, to, resourceObj, actionKeys.ToList(), by, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Remove resource from rightholder connection and all actions
    /// </summary>
    [HttpDelete("resources")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveResource([FromQuery] ConnectionInput connection, [FromQuery] string resource, CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateRemoveResourceFromConnection(connection.Party, connection.From, connection.To));

        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var byId = AuthenticationHelper.GetPartyUuid(this.HttpContext);
        if (!Guid.TryParse(connection.From, out var fromId) || !Guid.TryParse(connection.To, out var toId) || byId == Guid.Empty)
        {
            return Problem();
        }

        var problem = await ConnectionService.RemoveResource(fromId, toId, resource, ConfigureConnections, cancellationToken);
        
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>
    /// Delegation check of resources, for which resources the authenticated user has permission to assign to others on behalf of the specified party.
    /// </summary>
    [HttpGet("resources/delegationcheck")]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<PaginatedResult<ResourceDto.ResourceDtoCheck>>(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckResource([FromQuery] Guid party, [FromQuery] string resource, [FromBody] string[] actionKeys, CancellationToken cancellationToken = default)
    {
        return NotFound();
    }

    #endregion
}
