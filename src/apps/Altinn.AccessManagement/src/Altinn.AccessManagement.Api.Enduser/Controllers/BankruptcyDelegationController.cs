using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Services;
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
    [Tags("Bankruptcy Estate Delegation")]
    public class BankruptcyDelegationController(
        IInputValidation inputValidation,
        IBankruptcyEstateDelegationService bankruptcyDelegationService) : ControllerBase
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

        private Action<SanitizeOptions> ConfigureInputValidationUser { get; } = options =>
        {
            options.AllowedToEntityTypes = [EntityTypeConstants.Person];
            options.EntitiesToValidateForAnyConnections = [EntityTypeConstants.Person];
            options.ToParameterName = "user";
        };

        [HttpGet("estates/any")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [ProducesResponseType<bool>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> HasBankruptcyEstates(
            [FromQuery(Name = "party")][Required] Guid party,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.HasBankruptcyEstates(party, cancellationToken);

            return Ok(result);
        }

        [HttpGet("administrators")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_READ)]
        [ProducesResponseType<PaginatedResult<BankruptcyEstateAdministratorDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAgentAdminInformation(
            [FromQuery(Name = "party")][Required] Guid party,
            CancellationToken cancellationToken = default)
        {
            var result = await bankruptcyDelegationService.GetBankruptcyEstateAdministrators(party, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(PaginatedResult.Create(result.Value, null));
        }

        [HttpPost("administrators")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType<AssignmentDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AddBankruptcyEstateAdministrator(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromBody] PersonInput? person,
            CancellationToken cancellationToken = default)
        {
            var entity = await inputValidation.SanitizeToInput(
            party,
            null,
            person,
            ConfigureInputValidationUser,
            cancellationToken);

            if (entity.IsProblem)
            {
                return entity.Problem.ToActionResult();
            }

            var result = await bankruptcyDelegationService.AddBankruptcyEstateAdministrator(party, entity.Value.Id, ConfigureBankruptcyUserConnections, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        [HttpDelete("administrators")]
        [HttpPost("administrators/delete")]
        [Authorize(Policy = AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE)]
        [Authorize(Policy = AuthzConstants.POLICY_BANKRUPTCYDELEGATION_WRITE)]
        [AuditJWTClaimToDb(Claim = AltinnCoreClaimTypes.PartyUuid, System = AuditDefaults.EnduserApi)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokeBankruptcyEstateAdministrator(
            [FromQuery(Name = "party")][Required] Guid party,
            [FromQuery(Name = "user")][Required] Guid user,
            CancellationToken cancellationToken = default)
        {
            var problem = await bankruptcyDelegationService.RevokeBankruptcyEstateAdministrator(party, user, ConfigureBankruptcyUserConnections, cancellationToken);
            if (problem is not null)
            {
                return problem.ToActionResult();
            }

            return NoContent();
        }
    }
}
