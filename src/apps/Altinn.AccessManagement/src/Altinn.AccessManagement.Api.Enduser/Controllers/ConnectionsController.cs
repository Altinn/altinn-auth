using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
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
[FeatureGate(AccessMgmtFeatureFlags.EnduserControllerConnections)]
public class ConnectionsController(
    IConnectionService ConnectionService,
    IInputValidation inputValidation,
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
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [ProducesResponseType<PaginatedResult<ConnectionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConnections(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "from")] Guid? from,
        [FromQuery(Name = "to")] Guid? to,
        [FromQuery, FromHeader] PagingInput paging,
        [FromQuery] bool includeClientDelegations = true,
        [FromQuery] bool includeAgentConnections = true,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(ConnectionValidation.ValidateReadConnection(party.ToString(), from?.ToString(), to?.ToString()));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var result = await ConnectionService.Get(
            party,
            from,
            to,
            includeClientDelegations,
            includeAgentConnections,
            ConfigureConnections,
            cancellationToken
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    /// <summary>
    /// Gets all available users who already have some access from the specified party and are available to receive new delegations.
    /// </summary>
    [HttpGet("users")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_INSTANCE_DELEGATION)]
    [ProducesResponseType<PaginatedResult<SimplifiedConnectionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAvailableUsers(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ParameterValidation.Party(party.ToString()),
            ConnectionValidation.ValidateReadConnection(party.ToString(), party.ToString(), null));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var result = await ConnectionService.Get(
            party,
            party,
            null,
            includeClientDelegations: true,
            includeAgentConnections: true,
            ConfigureConnections,
            cancellationToken
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        var simplifiedConnections = DtoMapper.ToSimplifiedConnections(result.Value);
        return Ok(PaginatedResult.Create(simplifiedConnections, null));
    }

    #region Assignment

    /// <summary>
    /// Adds a new rightholder connection.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AddRightholder(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "to")] Guid to,
        [FromBody] PersonInput? person,
        CancellationToken cancellationToken = default)
    {
        var entity = await inputValidation.SanitizeToInput(
            party,
            to,
            person,
            options =>
            {
                options.AllowedToEntityTypes = [EntityTypeConstants.Person, EntityTypeConstants.Organization];
                options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
            },
            cancellationToken);

        if (entity.IsProblem)
        {
            return entity.Problem.ToActionResult();
        }

        var result = await ConnectionService.AddRightholder(party, entity.Value.Id, ConfigureConnections, cancellationToken);
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
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDIRECTIONAL_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveAssignment(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "from")] Guid from,
        [Required][FromQuery(Name = "to")] Guid to,
        [FromQuery(Name = "cascade")] bool cascade = false,
        CancellationToken cancellationToken = default)
    {
        var problem = await ConnectionService.RemoveAssignment(from, to, cascade, ConfigureConnections, cancellationToken);
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
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<PackagePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPackages(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "from")] Guid? from,
        [FromQuery(Name = "to")] Guid? to,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await ConnectionService.GetPackages(party, from, to, ConfigureConnections, cancellationToken);
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
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<AssignmentPackageDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddAssignmentPackage(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "to")] Guid to,
        [FromQuery(Name = "packageId")] Guid? packageId,
        [FromQuery(Name = "package")] string package,
        [FromBody] PersonInput? person,
        CancellationToken cancellationToken = default)
    {
        var entity = await inputValidation.SanitizeToInput(
            party,
            to,
            person,
            options =>
            {
                options.AllowedToEntityTypes = [EntityTypeConstants.Person, EntityTypeConstants.Organization];
                options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
            },
            cancellationToken);

        var result = await AddPackage();
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);

        async Task<Result<AssignmentPackageDto>> AddPackage()
        {
            if (packageId.HasValue)
            {
                return await ConnectionService.AddPackage(party, to, packageId.Value, ConfigureConnections, cancellationToken);
            }

            return await ConnectionService.AddPackage(party, to, package, ConfigureConnections, cancellationToken);
        }
    }

    /// <summary>
    /// Remove package from rightholder connection
    /// </summary>
    [HttpDelete("accesspackages")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDIRECTIONAL_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemovePackages(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "from")] Guid from,
        [Required][FromQuery(Name = "to")] Guid to,
        [FromQuery(Name = "packageId")] Guid? packageId,
        [FromQuery(Name = "package")] string package,
        CancellationToken cancellationToken = default)
    {
        var problem = await RemovePackage();

        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();

        async Task<ValidationProblemInstance> RemovePackage()
        {
            if (packageId.HasValue)
            {
                return await ConnectionService.RemovePackage(from, to, packageId.Value, ConfigureConnections, cancellationToken);
            }

            return await ConnectionService.RemovePackage(from, to, package, ConfigureConnections, cancellationToken);
        }
    }

    /// <summary>
    /// Delegation check of access packages, for which packages the authenticated user has permission to assign to others on behalf of the specified party.
    /// </summary>
    [HttpGet("accesspackages/delegationcheck")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<AccessPackageDto.AccessPackageDtoCheck>>(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckPackage(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "packageIds")] IEnumerable<Guid>? packageIds,
        [FromQuery(Name = "packages")] IEnumerable<string>? packages,
        CancellationToken cancellationToken = default)
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
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<PaginatedResult<RolePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "from")] Guid from,
        [Required][FromQuery(Name = "to")] Guid to,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var result = await ConnectionService.GetRoles(party, from, to, ConfigureConnections, cancellationToken);
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
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDIRECTIONAL_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ApiExplorerSettings(IgnoreApi = true)] //// Should stay hidden/closed until role service is ready.
    public async Task<IActionResult> RemoveRole(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "to")] Guid to,
        [FromQuery] string roleCode,
        CancellationToken cancellationToken = default)
    {
        return NotFound();

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

    /// <summary>
    /// Delegation check of roles, for which roles the authenticated user has permissions for on behalf of the specified party.
    /// </summary>
    [HttpGet("roles/delegationcheck")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ApiExplorerSettings(IgnoreApi = true)] //// Should stay hidden/closed in APIM unless we later on need to open for role delegation for endusers
    [ProducesResponseType<PaginatedResult<RoleDtoCheck>>(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DelegationCheckRoles(
        [Required][FromQuery(Name = "party")] Guid party,
        CancellationToken cancellationToken = default)
    {
        async Task<Result<IEnumerable<RoleDtoCheck>>> CheckRoles()
        {
            return await ConnectionService.RoleDelegationCheck(party, cancellationToken: cancellationToken);
        }

        var result = await CheckRoles();
        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    #endregion

    #region Resources

    /// <summary>
    /// Gets all resources between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet("resources")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<IEnumerable<ResourcePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResources(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "from")] Guid? from,
        [FromQuery(Name = "to")] Guid? to,
        [FromQuery, FromHeader] PagingInput paging,
        [FromQuery] string? resource = null,
        CancellationToken cancellationToken = default)
    {
        Resource resourceObj = null;
        if (resource != null)
        {
            resourceObj = await resourceService.GetResource(resource, cancellationToken);
            if (resourceObj is null)
            {
                ProblemDetails problem = Core.Errors.Problems.InvalidResource.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                return problem.ToActionResult();
            }
        }

        var result = await ConnectionService.GetResources(
            party,
            fromId: from,
            toId: to,
            resourceId: resourceObj?.Id,
            configureConnections: ConfigureConnections,
            cancellationToken: cancellationToken
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all resources between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet("resources/rights")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<ExternalResourceRightDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetResourceRights(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "from")] Guid from,
        [Required][FromQuery(Name = "to")] Guid to,
        [Required][FromQuery(Name = "resource")] string resource,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var resourceObj = await resourceService.GetResource(resource, cancellationToken);
        if (resourceObj is null)
        {
            ProblemDetails problem = Core.Errors.Problems.InvalidResource.ToProblemDetails();
            problem.Extensions["resource"] = resource;
            return problem.ToActionResult();
        }

        var result = party == from
            ? await ConnectionService.GetResourceRightsToOthers(
                partyId: party,
                toId: to,
                resourceId: resourceObj.Id,
                configureConnection: ConfigureConnections,
                cancellationToken: cancellationToken
                )
            : await ConnectionService.GetResourceRightsFromOthers(
                partyId: party,
                fromId: from,
                resourceId: resourceObj.Id,
                configureConnection: ConfigureConnections,
                cancellationToken: cancellationToken
                );

        var externalResult = new ExternalResourceRightDto
        {
            Resource = DtoMapper.Convert(resourceObj),
            DirectRights = [],
            IndirectRights = []
        };

        foreach (var right in result?.Rights ?? [])
        {
            if (right.Reason.Contains(AccessReasonFlag.Direct))
            {
                RightPermission rightPermission = new RightPermission
                {
                    Right = right.Right,
                    Reason = AccessReasonFlag.Direct,
                    Permissions = right.Permissions.Where(p => p.Reason == AccessReasonFlag.Direct).ToList()
                };
                externalResult.DirectRights.Add(rightPermission);
            }

            // if the right contains any other reason than Direct, we consider it an indirect right and include it in the IndirectRights list
            if (right.Reason != AccessReasonFlag.Direct)
            {
                RightPermission rightPermission = new RightPermission
                {
                    Right = right.Right,
                    Reason = right.Reason & ~AccessReasonFlag.Direct, // Remove Direct flag from reason for indirect rights
                    Permissions = right.Permissions.Where(p => p.Reason != AccessReasonFlag.Direct).ToList()
                };
                externalResult.IndirectRights.Add(rightPermission);
            }
        }

        return Ok(externalResult);
    }

    /// <summary>
    /// Add resource to an existing rightholder connection
    /// </summary>
    [HttpPost("resources/rights")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddResourceRights(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "to")] Guid to,
        [Required][FromQuery(Name = "resource")] string resource,
        [FromBody] RightKeyListDto rightKeys,
        CancellationToken cancellationToken = default)
    {
        var byId = AuthenticationHelper.GetPartyUuid(HttpContext);
        var fromEntity = await EntityService.GetEntity(party, cancellationToken);
        var toEntity = await EntityService.GetEntity(to, cancellationToken);
        var by = await EntityService.GetEntity(byId, cancellationToken);
        var resourceObj = await resourceService.GetResource(resource, cancellationToken);
        var result = await ConnectionService.AddResource(fromEntity, toEntity, resourceObj, rightKeys, by, ConfigureConnections, cancellationToken);

        if (result.IsProblem)
        {
            if (result.Problem.Equals(Core.Errors.Problems.InvalidResource))
            {
                ProblemDetails problem = result.Problem.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                return problem.ToActionResult();
            }

            return result.Problem.ToActionResult();
        }

        return Created();
    }

    /// <summary>
    /// Update resource to an existing rightholder connection
    /// </summary>
    [HttpPut("resources/rights")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateResourceRights(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "to")] Guid to,
        [Required][FromQuery(Name = "resource")] string resource,
        [FromBody] RightKeyListDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var byId = AuthenticationHelper.GetPartyUuid(HttpContext);
        var fromEntity = await EntityService.GetEntity(party, cancellationToken);
        var toEntity = await EntityService.GetEntity(to, cancellationToken);
        var byEntity = await EntityService.GetEntity(byId, cancellationToken);
        var resourceObj = await resourceService.GetResource(resource, cancellationToken);

        var result = await ConnectionService.UpdateResource(fromEntity, toEntity, resourceObj, updateDto.DirectRightKeys, byEntity, ConfigureConnections, cancellationToken);

        if (result.IsProblem)
        {
            if (result.Problem.Equals(Core.Errors.Problems.InvalidResource))
            {
                ProblemDetails problem = result.Problem.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                return problem.ToActionResult();
            }

            return result.Problem.ToActionResult();
        }

        return Ok();
    }

    /// <summary>
    /// Remove resource from rightholder connection and all actions
    /// </summary>
    [HttpDelete("resources")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDIRECTIONAL_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveResource(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "from")] Guid from,
        [Required][FromQuery(Name = "to")] Guid to,
        [FromQuery(Name = "resource")] string resource,
        CancellationToken cancellationToken = default)
    {
        var byId = AuthenticationHelper.GetPartyUuid(HttpContext);
        var problem = await ConnectionService.RemoveResource(from, to, resource, ConfigureConnections, cancellationToken);

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
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [ProducesResponseType<ResourceCheckDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckResource(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "resource")] string resource,
        CancellationToken cancellationToken = default)
    {
        Guid authenticatedUserUuid = AuthenticationHelper.GetPartyUuid(HttpContext);
        string languageCode = this.GetLanguageCode();

        var result = await ConnectionService.ResourceDelegationCheck(authenticatedUserUuid, party, resource, ConfigureConnections, languageCode, cancellationToken: cancellationToken);
        if (result.IsProblem)
        {
            if (result.Problem.Equals(Core.Errors.Problems.InvalidResource))
            {
                ProblemDetails problem = result.Problem.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                return problem.ToActionResult();
            }

            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    #endregion

    #region Instances

    /// <summary>
    /// Gets all resource instances between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet("resources/instances")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<IEnumerable<InstancePermissionDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInstances(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "from")] Guid? from,
        [FromQuery(Name = "to")] Guid? to,
        [FromQuery, FromHeader] PagingInput paging,
        [FromQuery] string? resource = null,
        [FromQuery] string? instance = null,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateReadConnection(party.ToString(), from?.ToString(), to?.ToString()),
            ParameterValidation.InstanceUrn(instance ?? string.Empty));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        Resource resourceObj = null;
        if (resource != null)
        {
            resourceObj = await resourceService.GetResource(resource, cancellationToken);
            if (resourceObj is null)
            {
                ProblemDetails problem = Core.Errors.Problems.InvalidResource.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                problem.Extensions["instance"] = instance;
                return problem.ToActionResult();
            }
        }

        var result = await ConnectionService.GetResourceInstances(
            party,
            fromId: from,
            toId: to,
            resourceId: resourceObj?.Id,
            instanceId: instance,
            configureConnections: ConfigureConnections,
            cancellationToken: cancellationToken
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        return Ok(PaginatedResult.Create(result.Value, null));
    }

    /// <summary>
    /// Gets all resource instance rights between the authenticated user's selected party and the specified target party.
    /// </summary>
    [HttpGet("resources/instances/rights")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType<ExtInstanceRightDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInstanceRights(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "from")] Guid from,
        [Required][FromQuery(Name = "to")] Guid to,
        [Required][FromQuery(Name = "resource")] string resource,
        [Required][FromQuery(Name = "instance")] string instance,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ConnectionValidation.ValidateReadConnection(party.ToString(), from.ToString(), to.ToString()),
            ParameterValidation.InstanceUrn(instance));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var resourceObj = await resourceService.GetResource(resource, cancellationToken);
        if (resourceObj is null)
        {
            ProblemDetails problem = Core.Errors.Problems.InvalidResource.ToProblemDetails();
            problem.Extensions["resource"] = resource;
            problem.Extensions["instance"] = instance;
            return problem.ToActionResult();
        }

        var result = party == from
            ? await ConnectionService.GetInstanceRightsToOthers(
                partyId: party,
                toId: to,
                resourceId: resourceObj.Id,
                instanceId: instance,
                configureConnection: ConfigureConnections,
                cancellationToken: cancellationToken
                )
            : await ConnectionService.GetInstanceRightsFromOthers(
                partyId: party,
                fromId: from,
                resourceId: resourceObj.Id,
                instanceId: instance,
                configureConnection: ConfigureConnections,
                cancellationToken: cancellationToken
                );

        var externalResult = new ExtInstanceRightDto
        {
            Resource = DtoMapper.Convert(resourceObj),
            Instance = result?.Instance,
            DirectRights = [],
            IndirectRights = []
        };

        foreach (var right in result?.Rights ?? [])
        {
            if (right.Reason.Contains(AccessReasonFlag.Direct))
            {
                RightPermission rightPermission = new RightPermission
                {
                    Right = right.Right,
                    Reason = AccessReasonFlag.Direct,
                    Permissions = right.Permissions.Where(p => p.Reason == AccessReasonFlag.Direct).ToList()
                };
                externalResult.DirectRights.Add(rightPermission);
            }

            // if the right contains any other reason than Direct, we consider it an indirect right and include it in the IndirectRights list
            if (right.Reason != AccessReasonFlag.Direct)
            {
                RightPermission rightPermission = new RightPermission
                {
                    Right = right.Right,
                    Reason = right.Reason & ~AccessReasonFlag.Direct, // Remove Direct flag from reason for indirect rights
                    Permissions = right.Permissions.Where(p => p.Reason != AccessReasonFlag.Direct).ToList()
                };
                externalResult.IndirectRights.Add(rightPermission);
            }
        }

        return Ok(externalResult);
    }

    /// <summary>
    /// Add resource instance rights to an existing or new rightholder connection.
    /// Supports two modes: 
    /// 1. Using 'to' query parameter for existing connections
    /// 2. Using PersonInput in body to create new rightholder and delegate in one operation
    /// </summary>
    [HttpPost("resources/instances/rights")]
    [FeatureGate(AccessMgmtFeatureFlags.InstanceDbEf)]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_INSTANCE_DELEGATION)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddInstanceRights(
        [Required][FromQuery(Name = "party")] Guid party,
        [FromQuery(Name = "to")] Guid? to,
        [Required][FromQuery(Name = "resource")] string resource,
        [Required][FromQuery(Name = "instance")] string instance,
        [Required][FromBody] InstanceRightsDelegationDto input,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(ParameterValidation.InstanceUrn(instance));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        // Resolve the target entity
        Guid targetEntityId;
        if (to.HasValue)
        {
            // Existing connection scenario
            targetEntityId = to.Value;
        }
        else
        {
            // New rightholder scenario - use inputValidation to sanitize and create/get entity
            var personInput = new PersonInput
            {
                PersonIdentifier = input.To.PersonIdentifier,
                LastName = input.To.LastName
            };

            var entityResult = await inputValidation.SanitizeToInput(
                party,
                Guid.Empty, // Will be resolved from PersonInput
                personInput,
                options =>
                {
                    options.AllowedToEntityTypes = [EntityTypeConstants.Person, EntityTypeConstants.Organization];
                    options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
                },
                cancellationToken);

            if (entityResult.IsProblem)
            {
                return entityResult.Problem.ToActionResult();
            }

            targetEntityId = entityResult.Value.Id;

            // Create rightholder connection (AddRightholder returns existing connection if already exists)
            var assignmentResult = await ConnectionService.AddRightholder(party, targetEntityId, ConfigureConnections, cancellationToken);
            if (assignmentResult.IsProblem)
            {
                return assignmentResult.Problem.ToActionResult();
            }
        }

        // Proceed with instance delegation
        var byId = AuthenticationHelper.GetPartyUuid(HttpContext);
        var fromEntity = await EntityService.GetEntity(party, cancellationToken);
        var toEntity = await EntityService.GetEntity(targetEntityId, cancellationToken);
        var by = await EntityService.GetEntity(byId, cancellationToken);
        var resourceObj = await resourceService.GetResource(resource, cancellationToken);

        // Convert input to RightKeyListDto for service call
        var rightKeysDto = new RightKeyListDto { DirectRightKeys = input.DirectRightKeys };

        var result = await ConnectionService.AddInstance(fromEntity, toEntity, resourceObj, instance, rightKeysDto, by, ConfigureConnections, cancellationToken);

        if (result.IsProblem)
        {
            if (result.Problem.Equals(Core.Errors.Problems.InvalidResource))
            {
                ProblemDetails problem = result.Problem.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                problem.Extensions["instance"] = instance;
                return problem.ToActionResult();
            }

            return result.Problem.ToActionResult();
        }

        return Created();
    }

    /// <summary>
    /// Update resource instance rights for an existing rightholder connection
    /// </summary>
    [HttpPut("resources/instances/rights")]
    [FeatureGate(AccessMgmtFeatureFlags.InstanceDbEf)]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInstanceRights(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "to")] Guid to,
        [Required][FromQuery(Name = "resource")] string resource,
        [Required][FromQuery(Name = "instance")] string instance,
        [FromBody] RightKeyListDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(ParameterValidation.InstanceUrn(instance));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var byId = AuthenticationHelper.GetPartyUuid(HttpContext);
        var fromEntity = await EntityService.GetEntity(party, cancellationToken);
        var toEntity = await EntityService.GetEntity(to, cancellationToken);
        var byEntity = await EntityService.GetEntity(byId, cancellationToken);
        var resourceObj = await resourceService.GetResource(resource, cancellationToken);

        var result = await ConnectionService.UpdateInstance(fromEntity, toEntity, resourceObj, instance, updateDto.DirectRightKeys, byEntity, ConfigureConnections, cancellationToken);

        if (result.IsProblem)
        {
            if (result.Problem.Equals(Core.Errors.Problems.InvalidResource))
            {
                ProblemDetails problem = result.Problem.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                problem.Extensions["instance"] = instance;
                return problem.ToActionResult();
            }

            return result.Problem.ToActionResult();
        }

        return Ok();
    }

    /// <summary>
    /// Remove resource instance from rightholder connection and all actions
    /// </summary>
    [HttpDelete("resources/instances")]
    [FeatureGate(AccessMgmtFeatureFlags.InstanceDbEf)]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDIRECTIONAL_WRITE)]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE)]
    [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveInstance(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "from")] Guid from,
        [Required][FromQuery(Name = "to")] Guid to,
        [Required][FromQuery(Name = "resource")] string resource,
        [Required][FromQuery(Name = "instance")] string instance,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(ParameterValidation.InstanceUrn(instance));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var problem = await ConnectionService.RemoveInstance(from, to, resource, instance, ConfigureConnections, cancellationToken);
        if (problem is { })
        {
            return problem.ToActionResult();
        }

        return NoContent();
    }

    /// <summary>
    /// Delegation check of instance delegation, for which resources the authenticated user has permission to assign to others on behalf of the specified party.
    /// </summary>
    [HttpGet("resources/instances/delegationcheck")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_WRITE_TOOTHERS)]
    [Authorize(Policy = AuthzConstants.POLICY_INSTANCE_DELEGATION)]
    [ProducesResponseType<InstanceCheckDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckInstance(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "resource")] string resource,
        [Required][FromQuery(Name = "instance")] string instance,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(ParameterValidation.InstanceUrn(instance));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        Guid authenticatedUserUuid = AuthenticationHelper.GetPartyUuid(HttpContext);
        string languageCode = this.GetLanguageCode();

        var result = await ConnectionService.InstanceDelegationCheck(authenticatedUserUuid, party, resource, instance, ConfigureConnections, languageCode, cancellationToken);
        if (result.IsProblem)
        {
            if (result.Problem.Equals(Core.Errors.Problems.InvalidResource))
            {
                ProblemDetails problem = result.Problem.ToProblemDetails();
                problem.Extensions["resource"] = resource;
                problem.Extensions["instance"] = instance;
                return problem.ToActionResult();
            }

            return result.Problem.ToActionResult();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all users who have access to a specific instance.
    /// </summary>
    [HttpGet("resources/instances/users")]
    [Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_BIDRECTIONAL_READ)]
    [Authorize(Policy = AuthzConstants.POLICY_INSTANCE_DELEGATION)]
    [ProducesResponseType<PaginatedResult<SimplifiedPartyDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInstanceUsers(
        [Required][FromQuery(Name = "party")] Guid party,
        [Required][FromQuery(Name = "resource")] string resource,
        [Required][FromQuery(Name = "instance")] string instance,
        [FromQuery, FromHeader] PagingInput paging,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidationComposer.Validate(
            ParameterValidation.Party(party.ToString()),
            ParameterValidation.InstanceUrn(instance));
        if (validationErrors is { })
        {
            return validationErrors.ToActionResult();
        }

        var resourceObj = await resourceService.GetResource(resource, cancellationToken);
        if (resourceObj is null)
        {
            ProblemDetails problem = Core.Errors.Problems.InvalidResource.ToProblemDetails();
            problem.Extensions["resource"] = resource;
            problem.Extensions["instance"] = instance;
            return problem.ToActionResult();
        }

        var result = await ConnectionService.GetResourceInstances(
            party,
            fromId: party,
            toId: null,
            resourceId: resourceObj.Id,
            instanceId: instance,
            configureConnections: ConfigureConnections,
            cancellationToken: cancellationToken
        );

        if (result.IsProblem)
        {
            return result.Problem.ToActionResult();
        }

        var users = result.Value
            .SelectMany(inst => inst.Permissions ?? Enumerable.Empty<PermissionDto>())
            .Where(perm => perm.To != null)
            .Select(perm => DtoMapper.ToSimplifiedParty(perm.To))
            .DistinctBy(p => p.Id)
            .ToList();

        return Ok(PaginatedResult.Create(users, null));
    }

    #endregion
}
