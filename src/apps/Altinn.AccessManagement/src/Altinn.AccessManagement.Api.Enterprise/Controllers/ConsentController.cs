using Altinn.AccessManagement.Api.Enterprise.Extensions;
using Altinn.AccessManagement.Api.Enterprise.Utils;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.PEP.Helpers;
using Altinn.Common.PEP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;

namespace Altinn.AccessManagement.Api.Enterprise.Controllers
{
    /// <summary>
    /// The default constructor taking in depencies. 
    /// </summary>
    [Route("accessmanagement/api/v1/enterprise")]
    [ApiController]
    public class ConsentController(IConsent consentService, IPDP Pdp, IOptionsMonitor<ConsentSettings> consentSettings) : ControllerBase
    {
        private readonly IConsent _consentService = consentService;
        private readonly IPDP _pdp = Pdp;
        private readonly IOptionsMonitor<ConsentSettings> _consentSettings = consentSettings;

        private const string CreateRouteName = "enterprisecreateconsentrequest";
        private const string GetRouteName = "enterprisegetconsentrequest";
        private const string ROUTE_CONSENTEVENTS = "consentrequests/events";

        /// <summary>
        /// Endpoint to create a consent request for
        /// </summary>
        [Authorize(Policy = AuthzConstants.POLICY_CONSENTREQUEST_WRITE)]
        [HttpPost]
        [Route("consentrequests", Name = CreateRouteName)]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ConsentRequestDetailsDto), StatusCodes.Status200OK)]
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

            // Authorize that the enterprise is auhtorized to request consent for the resources in the consent request
            foreach (ConsentRight right in consentRequestInternal.ConsentRights)
            {
                if (!await AuthorizeCreateConsentRequest(right.Resource[0].Value, User))
                {
                    return Forbid();
                }
            }

            Result<ConsentRequestDetailsWrapper> consentRequestStatus = await _consentService.CreateRequest(consentRequestInternal, consentPartyUrn, false, cancellationToken);

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
        [Route("consentrequests/{consentRequestId:guid}", Name = GetRouteName)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ConsentRequestDetailsDto), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Get a list of consent events for the authenticated enterprise.
        /// Results are ordered oldest first (ascending by event ID). When paginating, the oldest events are returned first and the newest last.
        /// Only events created more than 5 minutes ago are returned, to ensure consistency and avoid returning events that are still being processed.
        /// Uses cursor-based pagination via the <c>continuationToken</c> parameter. If a <c>nextLink</c> is present in the response, follow it to retrieve the next page.
        /// </summary>
        /// <param name="continuationToken">Opaque cursor token returned in the <c>nextLink</c> of a previous response. Pass this to retrieve the next page of results.</param>
        /// <param name="createdAfter">Optional. Filters events created at or after this timestamp.</param>
        /// <param name="createdBefore">Optional. Filters events created before this timestamp.</param>
        /// <param name="eventTypes">Optional. Filters results to one or more specific event types. Can be specified multiple times, e.g. <c>eventType=accepted&amp;eventType=revoked</c>.</param>
        /// <param name="consentRequestId">Optional. Filters results to events belonging to a specific consent request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [Authorize(Policy = AuthzConstants.POLICY_CONSENTREQUEST_READ)]
        [HttpGet]
        [Route("consentrequests/events", Name = ROUTE_CONSENTEVENTS)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PaginatedResult<ConsentStatusChangeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetConsentEvents(
            [FromQuery(Name = "continuationToken")] string? continuationToken = null,
            [FromQuery(Name = "createdAfter")] DateTimeOffset? createdAfter = null,
            [FromQuery(Name = "createdBefore")] DateTimeOffset? createdBefore = null,
            [FromQuery(Name = "eventType")] string[]? eventTypes = null,
            [FromQuery(Name = "consentRequestId")] Guid? consentRequestId = null, 
            CancellationToken cancellationToken = default)
        {
            int pageSize = _consentSettings.CurrentValue.EventsPageSize;

            Guid? continueFrom = null;
            Core.Models.Consent.ConsentPartyUrn? authenticatedParty = OrgUtil.GetAuthenticatedParty(User);

            if (authenticatedParty == null)
            {
                return Unauthorized();
            }

            ValidationErrorBuilder errors = default;

            if (createdAfter.HasValue && createdBefore.HasValue && createdAfter >= createdBefore)
            {
                errors.Add(ValidationErrors.InvalidDateRange, "$QUERY/createdAfter");
            }

            if (eventTypes is { Length: > 0 })
            {
                var validEventTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "rejected", "accepted", "revoked", "deleted", "used"
                };

                foreach (string eventType in eventTypes)
                {
                    if (!validEventTypes.Contains(eventType))
                    {
                        errors.Add(ValidationErrors.InvalidEventType, "$QUERY/eventType");
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(continuationToken))
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(continuationToken);                    
                    if (bytes.Length != 16)
                    {
                        errors.Add(ValidationErrors.InvalidContinuationToken, "$QUERY/continuationToken");
                    }
                    else
                    {
                        continueFrom = new Guid(bytes);
                    }                    
                }
                catch (FormatException)
                {
                    errors.Add(ValidationErrors.InvalidContinuationToken, "$QUERY/continuationToken");
                }
            }

            if (errors.TryBuild(out var errorResult))
            {
                return errorResult.ToActionResult();
            }

            ConsentEventsQuery query = new ConsentEventsQuery(
                ConsentRequestId: consentRequestId,
                EventTypes: eventTypes,
                CreatedAfter: createdAfter.HasValue ? createdAfter.Value.UtcDateTime : (DateTime?)null,
                CreatedBefore: createdBefore.HasValue ? createdBefore.Value.UtcDateTime : (DateTime?)null,
                ContinueFrom: continueFrom);

            Result<List<ConsentStatusChange>> result = await _consentService.GetConsentEventsForParty(authenticatedParty, query, pageSize, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            List<ConsentStatusChange> changes = result.Value;

            // Convert to DTOs
            List<ConsentStatusChangeDto> dtos = changes.Select(c => c.ToDto()).ToList();

            // Calculate next continuation token if there are more results
            string? nextLink = null;
            if (dtos.Count == pageSize)
            {
                Guid consentEventId = changes.Last().ConsentEventId;
                string nextToken = Convert.ToBase64String(consentEventId.ToByteArray());

                var routeValues = new RouteValueDictionary
                {
                    { "continuationToken", nextToken }
                };

                if (Request.Query.ContainsKey("consentRequestId"))
                {
                    routeValues["consentRequestId"] = query.ConsentRequestId?.ToString();
                }

                if (Request.Query.ContainsKey("createdAfter"))
                {
                    routeValues["createdAfter"] = query.CreatedAfter?.ToString("O");
                }

                if (Request.Query.ContainsKey("createdBefore"))
                {
                    routeValues["createdBefore"] = query.CreatedBefore?.ToString("O");
                }

                if (Request.Query.ContainsKey("eventType"))
                {
                    routeValues["eventType"] = query.EventTypes;
                }

                nextLink = Url.Link(ROUTE_CONSENTEVENTS, routeValues);
            }

            // Return paginated result
            return Ok(PaginatedResult.Create(dtos, nextLink));
        }

        private async Task<bool> AuthorizeCreateConsentRequest(string consentResource, ClaimsPrincipal claimsPrincipal)
        {
            XacmlJsonRequestRoot request = DecisionHelper.CreateDecisionRequestForResourceRegistryResource(consentResource, null, claimsPrincipal, AltinnXacmlConstants.MatchAttributeIdentifiers.RequestconsentAction);
            XacmlJsonResponse response = await _pdp.GetDecisionForRequest(request);

            if (response?.Response == null)
            {
                throw new InvalidOperationException("response");
            }

            if (!DecisionHelper.ValidatePdpDecisionWithoutObligationCheck(response.Response, claimsPrincipal))
            {
                return false;
            }

            return true;
        }
    }
}
