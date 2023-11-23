using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Interface to support various queries agaoim
    /// </summary>
    public interface IAltinn2Services
    {
        /// <summary>
        /// Return a list of Available services from Altinn 2
        /// </summary>
        public Task<List<AvailableService>> AvailableServices(int languageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a service resources based on a Altinn 2 service
        /// </summary>
        Task<ServiceResource> GetServiceResourceFromService(string serviceCode, int serviceEditionCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// GEts the xacml policy for a service
        /// </summary>
        Task<XacmlPolicy> GetXacmlPolicy(string serviceCode, int serviceEditionCode, string identifier, CancellationToken cancellationToken = default);
    }
}
