using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.Platform.Authorization.Services.Interface
{
    /// <summary>
    /// Defines Interface for Context Handler.
    /// </summary>
    public interface IContextHandler
    {
        /// <summary>
        /// Enrich the DecisionRequest with needed attributes so PDP can evaluate decision request for a policy/policyset.
        /// </summary>
        /// <param name="decisionRequest">The XacmlContextRequest.</param>
        /// <param name="isExternalRequest">Defines if call is comming from external source</param>
        /// <param name="appInstanceInfo">Cache of auth info for this request</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>Enriched context.</returns>
        Task<XacmlContextRequest> Enrich(XacmlContextRequest decisionRequest, bool isExternalRequest, SortedDictionary<string, AuthInfo> appInstanceInfo, CancellationToken cancellationToken = default);
    }
}
