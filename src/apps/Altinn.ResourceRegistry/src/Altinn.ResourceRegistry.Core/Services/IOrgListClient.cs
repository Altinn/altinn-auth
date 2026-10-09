using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Interface to describe the org service 
    /// </summary>
    public interface IOrgListClient
    {
        /// <summary>
        /// Fetches a list of orgs.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        public Task<OrgList> GetOrgList(CancellationToken cancellationToken = default);
    }
}
