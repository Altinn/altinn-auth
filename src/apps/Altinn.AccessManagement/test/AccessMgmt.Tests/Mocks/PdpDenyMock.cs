using System.Security.Claims;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Interfaces;

namespace Altinn.AccessManagement.Tests.Mocks
{
    /// <summary>
    /// Mock to delver Decision that is not Permit (deny)
    /// </summary>
    internal class PdpDenyMock : IPDP
    {
        /// <inheritdoc/>
        public Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest)
            => GetDecisionForRequest(xacmlJsonRequest, CancellationToken.None);

        /// <inheritdoc/>
        public Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new XacmlJsonResponse
            {
                Response = new List<XacmlJsonResult>(new[] { new XacmlJsonResult { Decision = "Indeterminate" } })
            };

            return Task.FromResult(response);
        }

        /// <inheritdoc/>
        public Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user)
            => GetDecisionForUnvalidateRequest(xacmlJsonRequest, user, CancellationToken.None);

        /// <inheritdoc/>
        public Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(false);
        }
    }
}
