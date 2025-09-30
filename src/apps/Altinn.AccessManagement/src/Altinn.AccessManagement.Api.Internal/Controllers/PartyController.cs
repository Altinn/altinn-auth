using System.IdentityModel.Tokens.Jwt;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.Authorization.Api.Contracts.Party;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Controllers
{
    [Authorize(Policy = AuthzConstants.PLATFORM_ACCESSTOKEN_ISSUER_ISPLATFORM)]
    [Route("accessmanagement/api/v1/internal/party")]
    public class PartyController : ControllerBase
    {
        private readonly IPartyService _partyService;

        public PartyController(IPartyService partyService)
        {
            _partyService = partyService;
        }

        [HttpPost]
        public async Task<ActionResult<AddPartyResultDto>> AddParty([FromBody] PartyBaseDto party, [FromHeader(Name = "PlatformAccessToken")] string token, CancellationToken cancellationToken = default)
        {
            if (!CheckValidAppClaim(token))
            {
                return Unauthorized("Invalid app claim in platform access token.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var res = await _partyService.AddParty(party, cancellationToken);

            if (res.IsProblem)
            {
                return res.Problem.ToActionResult();
            }

            return res.Value.PartyCreated
                ? CreatedAtAction(nameof(AddParty), new { id = res.Value.PartyUuid }, res.Value)
                : Ok(res.Value);
        }

        /// <summary>
        /// Validate app-claim from the platform token
        /// </summary>
        private static bool CheckValidAppClaim(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token);
                var appidentifier = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute);
                if (appidentifier != null)
                {
                    return appidentifier.Value.Equals("authentication", StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
    }
}
