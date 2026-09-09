using System.Net.Mime;
using Altinn.AccessManagement.Api.Internal.Utils;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.IdPortenAuthorization;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Internal.Controllers.Bff
{
    /// <summary>
    /// API controller for managing ID-porten authorizations for end users.
    /// All endpoints are accessible only from the Altinn Portal to ensure that end users are properly informed about the details of their ID-porten authorizations.
    /// The controller enforces the portal scope for authorization to access its methods.
    /// </summary>
    [Route("accessmanagement/api/v1/bff/idportenauthorization")]
    [ApiController]
    [Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
    public class IdPortenAuthorizationController(IIdPortenAuthorizationService idPortenAuthorizationService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<List<IdPortenAuthorization>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Route("")]
        public async Task<IActionResult> GetIdPortenAuthorizations(CancellationToken cancellationToken = default)
        {
            Guid? userUuid = UserUtil.GetUserUuid(User);
            if (userUuid == null)
            {
                return Unauthorized();
            }

            Result<List<IdPortenAuthorization>> result = await idPortenAuthorizationService.GetIdPortenAuthorizations(userUuid.Value, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        [HttpDelete]
        [ProducesResponseType<bool>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Route("")]
        public async Task<IActionResult> DeleteIdPortenAuthorization([FromQuery] string id, CancellationToken cancellationToken = default)
        {
            Guid? userUuid = UserUtil.GetUserUuid(User);
            if (userUuid == null)
            {
                return Unauthorized();
            }

            Result<bool> result = await idPortenAuthorizationService.DeleteIdPortenAuthorization(userUuid.Value, id, cancellationToken);
            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }
    }
}
