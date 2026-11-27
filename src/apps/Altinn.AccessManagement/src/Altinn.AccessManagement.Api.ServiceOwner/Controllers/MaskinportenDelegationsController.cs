#nullable enable
using Altinn.AccessManagement.Api.ServiceOwner.Models;
using Altinn.AccessManagement.Api.ServiceOwner.Utilities;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.Authorization.Integration.Platform.Samarbeidsportalen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.Api.ServiceOwner.Controllers
{
    /// <summary>
    /// Controller exposing the Maskinporten delegations lookup used by Maskinporten and service owners.
    /// </summary>
    [ApiController]
    [Route("accessmanagement/api/v1/")]
    public class MaskinportenDelegationsController : ControllerBase
    {
        private readonly ILogger<MaskinportenDelegationsController> _logger;
        private readonly IMaskinportenDelegationLookupService _delegation;
        private readonly IFeatureManager _featureManager;
        private readonly IScopePrefixValidator _scopePrefixValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskinportenDelegationsController"/> class.
        /// </summary>
        public MaskinportenDelegationsController(
            ILogger<MaskinportenDelegationsController> logger,
            IMaskinportenDelegationLookupService delegationLookupService,
            IFeatureManager featureManager,
            IScopePrefixValidator scopePrefixValidator)
        {
            _logger = logger;
            _delegation = delegationLookupService;
            _featureManager = featureManager;
            _scopePrefixValidator = scopePrefixValidator;
        }

        /// <summary>
        /// Endpoint for API owners and integration with Maskinporten for retrieval of information from Altinn 3 Access Management regarding active delegations of maskinporten schemas between organizations, giving access to one or more scopes in maskinporten.
        /// </summary>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet]
        [Route("maskinporten/delegations/")]
        [Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATIONS_PROXY)]
        [Produces("application/json")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<MPDelegationExternal>>> GetMaskinportenDelegations([FromQuery] string? supplierOrg, [FromQuery] string? consumerOrg, [FromQuery] string? scope, CancellationToken cancellationToken)
        {
            bool isAuthorized;
            try
            {
                isAuthorized = await IsAuthorizedDelegationLookupAccess(scope, cancellationToken);
            }
            catch (ScopePrefixDataUnavailableException ex)
            {
                // Fail closed: without the prefix list we can't tell which scopes the caller owns.
                _logger.LogError(ex, "GetMaskinportenDelegations could not check the scope against the prefixes the caller owns");
                return new ObjectResult(ProblemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Could not check which Maskinporten scope prefixes the caller owns"));
            }

            if (!isAuthorized)
            {
                ProblemDetails result;
                if (string.IsNullOrWhiteSpace(scope))
                {
                    result = new() { Title = $"Not authorized for lookup of delegations without specifying parameter: {nameof(scope)}", Status = StatusCodes.Status403Forbidden, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3" };
                }
                else
                {
                    result = new() { Title = $"Not authorized for lookup of delegations for the scope: {scope}", Status = StatusCodes.Status403Forbidden, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3" };
                }

                return StatusCode(StatusCodes.Status403Forbidden, result);
            }

            if (string.IsNullOrWhiteSpace(scope) && string.IsNullOrWhiteSpace(supplierOrg) && string.IsNullOrWhiteSpace(consumerOrg))
            {
                ProblemDetails result = new() { Title = $"Missing query parameters; provide at least one of: {nameof(scope)}, {nameof(supplierOrg)}, {nameof(consumerOrg)}", Status = StatusCodes.Status400BadRequest, Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1" };
                return StatusCode(StatusCodes.Status400BadRequest, result);
            }

            if (!string.IsNullOrEmpty(supplierOrg) && !IsValidOrganizationNumber(supplierOrg))
            {
                ModelState.AddModelError(nameof(supplierOrg), "supplierOrg is not a valid organization number");
                return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            if (!string.IsNullOrEmpty(consumerOrg) && !IsValidOrganizationNumber(consumerOrg))
            {
                ModelState.AddModelError(nameof(consumerOrg), "consumerOrg is not a valid organization number");
                return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }

            try
            {
                List<Delegation> delegations = await _delegation.GetMaskinportenDelegations(supplierOrg, consumerOrg, scope, cancellationToken);
                return delegations.Select(ToExternal).ToList();
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(ex.ParamName ?? "Validation Error", ex.Message);
                return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMaskinportenDelegation failed to fetch delegations");
                return new ObjectResult(ProblemDetailsFactory.CreateProblemDetails(HttpContext));
            }
        }

        /// <summary>
        /// Whether the caller may look up delegations of <paramref name="scope"/>. The admin scope may look up any scope.
        /// Others must own the scope's prefix: from Samarbeidsportalen when
        /// <see cref="AccessMgmtFeatureFlags.MaskinportenValidateScopePrefixWithApi"/> is on, otherwise from the
        /// <c>consumer_prefix</c> claims in the token.
        /// </summary>
        /// <exception cref="ScopePrefixDataUnavailableException">The flag is on and no usable prefix list is available.</exception>
        private async Task<bool> IsAuthorizedDelegationLookupAccess(string? scope, CancellationToken cancellationToken)
        {
            if (!await _featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.MaskinportenValidateScopePrefixWithApi))
            {
                return MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess(scope, User);
            }

            if (MaskinportenSchemaAuthorizer.HasDelegationsAdminScope(User))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(scope))
            {
                return false;
            }

            IReadOnlyList<string> unownedScopes = await _scopePrefixValidator.GetUnownedScopes(GetConsumerOrgNo(), [scope], cancellationToken);
            return unownedScopes.Count == 0;
        }

        /// <summary>
        /// Organization number of the caller, from the <c>consumer</c> claim, or <c>null</c> if it is missing or malformed.
        /// An organization number of <c>null</c> owns no prefixes.
        /// </summary>
        private string? GetConsumerOrgNo()
        {
            try
            {
                return OrgUtil.GetAuthenticatedParty(User) is ConsentPartyUrn.OrganizationId consumer
                    ? consumer.Value.ToString()
                    : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static MPDelegationExternal ToExternal(Delegation delegation)
        {
            Guid? delegationSchemeId = null;
            string? schemeReference = delegation.ResourceReferences?
                .Where(rf => rf.ReferenceType == ReferenceType.DelegationSchemeId && Guid.TryParse(rf.Reference, out _))
                .Select(rf => rf.Reference)
                .FirstOrDefault();
            if (schemeReference is not null)
            {
                delegationSchemeId = Guid.Parse(schemeReference);
            }

            HashSet<string> scopes = delegation.ResourceReferences?
                .Where(rf => rf.ReferenceType == ReferenceType.MaskinportenScope)
                .Select(rf => rf.Reference)
                .ToHashSet() ?? new HashSet<string>();

            return new MPDelegationExternal
            {
                SupplierOrg = delegation.CoveredByOrganizationNumber,
                ConsumerOrg = delegation.OfferedByOrganizationNumber,
#pragma warning disable CS0618 // Deprecated in the contract, but still returned until it is removed
                DelegationSchemeId = delegationSchemeId,
#pragma warning restore CS0618
                Scopes = scopes,
                Created = delegation.Created,
                ResourceId = delegation.ResourceId,
            };
        }

        private static bool IsValidOrganizationNumber(string orgNo)
        {
            if (string.IsNullOrEmpty(orgNo) || orgNo.Length != 9)
            {
                return false;
            }

            int[] weight = { 3, 2, 7, 6, 5, 4, 3, 2 };
            try
            {
                int sum = 0;
                for (int i = 0; i < orgNo.Length - 1; i++)
                {
                    sum += int.Parse(orgNo.Substring(i, 1)) * weight[i];
                }

                int ctrlDigit = 11 - (sum % 11);
                if (ctrlDigit == 11)
                {
                    ctrlDigit = 0;
                }

                return int.Parse(orgNo.Substring(orgNo.Length - 1)) == ctrlDigit;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
