using System.Security.Claims;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Clients;
using Altinn.Common.PEP.Helpers;
using Altinn.Common.PEP.Interfaces;
using Microsoft.Extensions.Logging;

namespace Altinn.Common.PEP.Implementation
{
    /// <summary>
    /// App implementation of the authorization service where the app uses the Altinn platform api.
    /// </summary>
    public class PDPAppSI : IPDP
    {
        private readonly ILogger _logger;
        private readonly AuthorizationApiClient _authorizationApiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PDPAppSI"/> class
        /// </summary>
        /// <param name="logger">the handler for logger service</param>
        /// <param name="authorizationApiClient">A typed Http client accessor</param>
        public PDPAppSI(ILogger<PDPAppSI> logger, AuthorizationApiClient authorizationApiClient)
        {
            _logger = logger;
            _authorizationApiClient = authorizationApiClient;
        }

        /// <inheritdoc/>
        public async Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest)
            => await GetDecisionForRequest(xacmlJsonRequest, CancellationToken.None);

        /// <inheritdoc/>
        public async Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest, CancellationToken cancellationToken)
        {
            XacmlJsonResponse xacmlJsonResponse = null;

            try
            {
                xacmlJsonResponse = await _authorizationApiClient.AuthorizeRequest(xacmlJsonRequest, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to retrieve Xacml Json response. An error occured {message}", e.Message);
            }

            return xacmlJsonResponse;
        }

        /// <inheritdoc/>
        public async Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user)
            => await GetDecisionForUnvalidateRequest(xacmlJsonRequest, user, CancellationToken.None);

        /// <inheritdoc/>
        public async Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            XacmlJsonResponse response = await GetDecisionForRequest(xacmlJsonRequest, cancellationToken);

            if (response?.Response == null)
            {
                throw new ArgumentNullException("response");
            }

            return DecisionHelper.ValidatePdpDecision(response.Response, user);
        }
    }
}
