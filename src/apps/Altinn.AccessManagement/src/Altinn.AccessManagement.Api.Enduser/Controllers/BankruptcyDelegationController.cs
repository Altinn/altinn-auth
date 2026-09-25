using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers
{
    [ApiController]
    [Route("accessmanagement/api/v1/enduser/bankruptcyestate")]
    [Tags("Bankruptcy Delegation")]
    public class BankruptcyDelegationController(
        IHttpContextAccessor httpContextAccessor,
        IInputValidation inputValidation,
        IBankruptcyDelegationService bankruptcyDelegationService,
        IConnectionService ConnectionService) : ControllerBase
    {
        private Action<ConnectionOptions> ConfigureBankruptcyUserConnections { get; } = options =>
        {
            options.AllowedWriteFromEntityTypes = [EntityTypeConstants.Person];
            options.AllowedWriteToEntityTypes = [EntityTypeConstants.Person];
            options.AllowedReadFromEntityTypes = [EntityTypeConstants.Person];
            options.AllowedReadToEntityTypes = [EntityTypeConstants.Person];
            options.FilterFromEntityTypes = [];
            options.FilterToEntityTypes = [];
        };

        private Action<ConnectionOptions> ConfigureCreditorConnections { get; } = options =>
        {
            options.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization];
            options.AllowedWriteToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
            options.AllowedReadFromEntityTypes = [EntityTypeConstants.Organization];
            options.AllowedReadToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
            options.FilterFromEntityTypes = [];
            options.FilterToEntityTypes = [];
        };

        private Action<SanitizeOptions> ConfigureInputValidationUser { get; } = options =>
        {
            options.AllowedToEntityTypes = [EntityTypeConstants.Person, EntityTypeConstants.Organization];
            options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
            options.ToParameterName = "user";
            options.PerformToEntityTypeValidation = true;
        };

        private Action<SanitizeOptions> ConfigureInputValidationCreditor { get; } = options =>
        {
            options.AllowedToEntityTypes = [EntityTypeConstants.Person, EntityTypeConstants.Organization];
            options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
            options.ToParameterName = "creditor";
            options.PerformToEntityTypeValidation = true;
        };

        /// <summary>
        /// Validates that a package list was supplied. An empty list carries no intent for either
        /// delegating or revoking access, and is rejected rather than silently creating or keeping
        /// a delegation without packages.
        /// </summary>
        /// <returns>A problem to return to the caller, or null when the list holds at least one package.</returns>
        private static ValidationProblemInstance ValidatePackagesNotEmpty(List<PackageReferenceDto> packages)
        {
            if (packages is { Count: > 0 })
            {
                return null;
            }

            ValidationErrorBuilder errorBuilder = default;
            errorBuilder.Add(
                ValidationErrors.Required,
                "/packages",
                [new("packages", "At least one package must be specified.")]);

            errorBuilder.TryBuild(out var problem);
            return problem;
        }

        private async Task<ValidationProblemInstance> ValidEstate(Guid party, Guid estate, CancellationToken cancellationToken)
        {
            var hasConnection = await bankruptcyDelegationService.CheckBankruptcyEstateConnection(party, estate, cancellationToken);

            if (!hasConnection)
            {
                ValidationErrorBuilder errorBuilder = default;
                errorBuilder.Add(
                    ValidationErrors.InvalidBankruptcyEstate,
                    "/estate",
                    [new("estate", $"The party has no connection to an estate with id {estate}.")]);

                errorBuilder.TryBuild(out var problem);
                return problem;
            }

            return null;
        }

        #region Creditor methods

        [HttpGet("estates/creditors")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType<PaginatedResult<CompactEntityDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCreditors(
           [FromQuery(Name = "party")][Required] Guid party,
           [FromQuery(Name = "estate")][Required] Guid estate,
           CancellationToken cancellationToken = default)
        {
            // Check that party has the estate as an active estate
            var validEstate = await ValidEstate(party, estate, cancellationToken);

            if (validEstate != null)
            {
                return validEstate.ToActionResult();
            }

            var result = await bankruptcyDelegationService.GetCreditors(party, estate, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(PaginatedResult.Create(result.Value, null));
        }

        [HttpPost("estates/creditors")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType<AssignmentWithAssignmentPackageDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AddCreditor(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "estate")][Required] Guid estate,
            [FromQuery(Name = "creditor")] Guid? creditor,
            [FromBody] PersonInput? person,
            CancellationToken cancellationToken = default)
        {
            // Check that party has the estate as an active estate
            var validEstate = await ValidEstate(party, estate, cancellationToken);

            if (validEstate != null)
            {
                return validEstate.ToActionResult();
            }

            var entity = await inputValidation.SanitizeToInput(
            estate,
            creditor,
            person,
            ConfigureInputValidationCreditor,
            cancellationToken);

            if (entity.IsProblem)
            {
                return entity.Problem.ToActionResult();
            }

            var result = await bankruptcyDelegationService.AddCreditor(party, estate, entity.Value.Id, ConfigureCreditorConnections, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        [HttpDelete("estates/creditors")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokeCreditor(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "estate")][Required] Guid estate,
            [FromQuery(Name = "creditor")][Required] Guid creditor,
            CancellationToken cancellationToken = default)
        {
            // Check that party has the estate as an active estate
            var validEstate = await ValidEstate(party, estate, cancellationToken);

            if (validEstate != null)
            {
                return validEstate.ToActionResult();
            }

            var result = await bankruptcyDelegationService.RevokeCreditor(party, estate, creditor, ConfigureCreditorConnections, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return NoContent();
        }

        #endregion

        #region Get agent/admin method

        [HttpGet("users")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [ProducesResponseType<PaginatedResult<BankruptcyEntityDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAgentAdminInformation(
            [FromQuery(Name = "party")][Required] Guid party,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.GetAgentAdminInformation(party, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(PaginatedResult.Create(result.Value, null));
        }

        #endregion

        #region Add/Revoke agent methods

        [HttpPost("users")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AddAgent(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "user")] Guid? user,
            [FromBody] PersonInput? person,
            CancellationToken cancellationToken = default)
        {
            var entity = await inputValidation.SanitizeToInput(
            party,
            user,
            person,
            ConfigureInputValidationUser,
            cancellationToken);

            if (entity.IsProblem)
            {
                return entity.Problem.ToActionResult();
            }

            var result = await bankruptcyDelegationService.AddAgent(party, entity.Value.Id, ConfigureBankruptcyUserConnections, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        [HttpDelete("users")]
        [HttpPost("users/delete")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokeAgent(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "user")][Required] Guid user,
            [FromQuery(Name = "cascade")] bool cascade = false,
            CancellationToken cancellationToken = default)
        {
            var problem = await bankruptcyDelegationService.RevokeAgent(party, user, cascade, ConfigureBankruptcyUserConnections, cancellationToken);
            if (problem is not null)
            {
                return problem.ToActionResult();
            }

            return NoContent();
        }

        #endregion

        #region Addd/Revoke admin methods

        [HttpPut("users/administrators")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType<AssignmentWithAssignmentPackageDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AddAdministrator(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "user")] Guid? user,
            [FromBody] PersonInput? person,
            CancellationToken cancellationToken = default)
        {
            var entity = await inputValidation.SanitizeToInput(
            party,
            user,
            person,
            ConfigureInputValidationUser,
            cancellationToken);

            if (entity.IsProblem)
            {
                return entity.Problem.ToActionResult();
            }

            var result = await bankruptcyDelegationService.AddAdministrator(party, entity.Value.Id, ConfigureBankruptcyUserConnections, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        [HttpDelete("users/administrators")]
        [HttpPost("users/administrators/delete")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokeAdministrator(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "user")][Required] Guid user,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.RevokeAdministrator(party, user, ConfigureBankruptcyUserConnections, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return NoContent();
        }

        #endregion

        #region Get bankruptcy estates

        [HttpGet("estates")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [ProducesResponseType<PaginatedResult<CompactEntityDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBankruptcyEstatesForParty(
            [FromQuery(Name = "party")][Required] Guid party,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.GetBankruptcyEstatesForParty(party, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(PaginatedResult.Create(result.Value, null));
        }

        [HttpGet]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [ProducesResponseType<bool>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> HasBankruptcyEstatesForParty(
            [FromQuery(Name = "party")][Required] Guid party,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.HasBankruptcyEstatesForParty(party, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        #endregion

        #region Bankruptcy estate get add revoke methods

        [HttpGet("estates/users")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [ProducesResponseType<PaginatedResult<CompactEntityDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBankruptcyEstatesForUser(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "user")][Required] Guid user,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.GetBankruptcyEstatesForUser(party, user, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(PaginatedResult.Create(result.Value, null));
        }

        [HttpGet("estates/users/packages")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [ProducesResponseType<PaginatedResult<PackageDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBankruptcyEstatePackagesForUser(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "estate")][Required] Guid estate,
            [FromQuery(Name = "user")][Required] Guid user,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.GetBankruptcyEstatePackagesForUser(party, estate, user, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(PaginatedResult.Create(result.Value, null));
        }

        [HttpPost("estates/users")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType<CreateDelegationResponseDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddBankruptcyEstateForUser(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "estate")][Required] Guid estate,
            [FromQuery(Name = "user")][Required] Guid user,
            [FromBody][Required] List<PackageReferenceDto> packages,
            CancellationToken cancellationToken = default)
        {
            // Check that party has the estate as an active estate
            var validEstate = await ValidEstate(party, estate, cancellationToken);

            if (validEstate != null)
            {
                return validEstate.ToActionResult();
            }

            var packagesProblem = ValidatePackagesNotEmpty(packages);
            if (packagesProblem is { })
            {
                return packagesProblem.ToActionResult();
            }

            var result = await bankruptcyDelegationService.AddBankruptcyEstateForUser(party, estate, user, packages, ConfigureBankruptcyUserConnections, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        [HttpDelete("estates/users")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokeBankruptcyEstateForUser(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "estate")][Required] Guid estate,
            [FromQuery(Name = "user")][Required] Guid user,
            [FromBody][Required] List<PackageReferenceDto> packages,
            CancellationToken cancellationToken = default)
        {
            // Check that party has the estate as an active estate
            var validEstate = await ValidEstate(party, estate, cancellationToken);

            if (validEstate != null)
            {
                return validEstate.ToActionResult();
            }

            var packagesProblem = ValidatePackagesNotEmpty(packages);
            if (packagesProblem is { })
            {
                return packagesProblem.ToActionResult();
            }

            var result = await bankruptcyDelegationService.RevokeBankruptcyEstateForUser(party, estate, user, packages, ConfigureBankruptcyUserConnections, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return NoContent();
        }

        #endregion
    }
}
