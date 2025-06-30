using System.Net.Mime;
using Altinn.AccessManagement.Api.Enterprise.Extensions;
using Altinn.AccessManagement.Api.Enterprise.Utils;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enterprise.Controllers
{
    /// <summary>
    /// The default constructor taking in depencies. 
    /// </summary>
    [Route("accessmanagement/api/v1/enterprise")]
    [ApiController]
    public class ConsentController(IConsent consentService) : ControllerBase
    {
        private readonly IConsent _consentService = consentService;

        private const string CreateRouteName = "enterprisecreateconsentrequest";
        private const string GetRouteName = "enterprisegetconsentrequest";

        /// <summary>
        /// Endpoint to create a consent request for
        /// </summary>
        [Authorize(Policy = AuthzConstants.POLICY_CONSENTREQUEST_WRITE)]
        [HttpPost]
        [Route("consentrequests", Name = CreateRouteName)]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ConsentRequestStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateRequest([FromBody] ConsentRequestDto consentRequest, CancellationToken cancellationToken = default)
        {
            Core.Models.Consent.ConsentPartyUrn? consentPartyUrn = OrgUtil.GetAuthenticatedParty(User);
            Core.Models.Consent.ConsentPartyUrn? supplierUrn = OrgUtil.GetSupplierParty(User);

            if (consentPartyUrn == null)
            {
                return Unauthorized();
            }

            ConsentRequest consentRequestInternal = ModelMapper.ToCore(consentRequest);
            if (supplierUrn != null)
            {
                consentRequestInternal.HandledBy = supplierUrn;
            }

            if (consentRequestInternal.To != consentPartyUrn)
            {
                // This scenario is only allowed for orgs creating consents for their own resources. 
                // Used in EBEVIS where Digdir request consent 
                string? scopes = OrgUtil.GetMaskinportenScopes(User);
                if (string.IsNullOrEmpty(scopes) || !scopes.Contains(AuthzConstants.SCOPE_CONSENTREQUEST_ORG))
                {
                    return Forbid();
                }

                consentRequestInternal.HandledBy = consentPartyUrn;
            }

            Result<ConsentRequestDetailsWrapper> consentRequestStatus = await _consentService.CreateRequest(consentRequestInternal, consentPartyUrn, cancellationToken);

            if (consentRequestStatus.IsProblem)
            {
                return consentRequestStatus.Problem.ToActionResult();
            }

            var routeValues = new { consentRequestId = consentRequestStatus.Value.ConsentRequest.Id };
            string? locationUrl = Url.Link(GetRouteName, routeValues);

            if (consentRequestStatus.Value.AlreadyExisted)
            {
                return Ok(consentRequestStatus.Value.ConsentRequest);
            }

            return Created(locationUrl, consentRequestStatus.Value.ConsentRequest.ToConsentRequestDetailsExternal());
        }

        /// <summary>
        /// Returns the consent request. Only returns request details for the authenticated party.
        /// </summary>
        [Authorize(Policy = AuthzConstants.POLICY_CONSENTREQUEST_READ)]
        [HttpGet]
        [Route("consentrequests/{consentRequestId:guid}", Name= GetRouteName)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ConsentRequestStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetRequest([FromRoute] Guid consentRequestId, CancellationToken cancellationToken = default)
        {
            Core.Models.Consent.ConsentPartyUrn? consentPartyUrn = OrgUtil.GetAuthenticatedParty(User);

            if (consentPartyUrn == null)
            {
                return Unauthorized();
            }
            
            Result<ConsentRequestDetails> consentRequestStatus = await _consentService.GetRequest(consentRequestId, consentPartyUrn, false, cancellationToken);

            if (consentRequestStatus.IsProblem)
            {
                return consentRequestStatus.Problem.ToActionResult();
            }

            return Ok(consentRequestStatus.Value.ToConsentRequestDetailsExternal());
        }
    }
}
