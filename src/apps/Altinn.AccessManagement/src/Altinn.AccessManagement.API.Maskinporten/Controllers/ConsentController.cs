using Altinn.AccessManagement.Api.Maskinporten.Extensions;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Maskinporten.Controllers
{
    /// <summary>
    /// Consent API for Maskinporten. Used to lookup consent information.
    /// </summary>
    /// <remarks>
    /// The default constructor taking in depencies. 
    /// </remarks>
    [Route("accessmanagement/api/v1/maskinporten/consent")]
    [ApiController]
    public class ConsentController(IConsent consentService) : ControllerBase
    {
        private readonly IConsent _consentService = consentService;

        /// <summary>
        /// Returns a specific consent based on consent id an from party
        /// </summary>
        [HttpPost]
        [Route("lookup")]
        [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_CONSENT_READ)]
        public async Task<ActionResult<ConsentInfoMaskinportenDto>> GetConsent([FromBody] ConsentLookupDto consentLookup, CancellationToken cancellationToken = default)
        {
            Result<Consent> consent = await _consentService.GetConsent(consentLookup.Id, MapToCore(consentLookup.From), MapToCore(consentLookup.To), cancellationToken);

            if (consent.IsProblem)
            {
                return consent.Problem.ToActionResult();
            }

            return Ok(consent.Value.ToConsentInfoMaskinporten());
        }

        private static Core.Models.Consent.ConsentPartyUrn MapToCore(Authorization.Api.Contracts.Consent.ConsentPartyUrn consentPartyUrnExternal)
        {
            Core.Models.Consent.ConsentPartyUrn consentPartyUrn = consentPartyUrnExternal switch
            {
                Authorization.Api.Contracts.Consent.ConsentPartyUrn.PersonId personUrn => Core.Models.Consent.ConsentPartyUrn.PersonId.Create(personUrn.Value),
                Authorization.Api.Contracts.Consent.ConsentPartyUrn.OrganizationId organizationUrn => Core.Models.Consent.ConsentPartyUrn.OrganizationId.Create(organizationUrn.Value),
                _ => throw new ArgumentException("Unknown consent party urn")
            };

            return consentPartyUrn;
        }
    }
}
