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
        {
            var response = new XacmlJsonResponse
            {
                Response = new List<XacmlJsonResult>(new[] { new XacmlJsonResult { Decision = "Indeterminate" } })
            };

            return Task.FromResult(response);
        }

        /// <inheritdoc/>
        public Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user)
        {
            return Task.FromResult(false);
        }
    }
}
