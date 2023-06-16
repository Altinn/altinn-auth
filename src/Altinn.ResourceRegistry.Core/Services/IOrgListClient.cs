using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Interface to describe the org service 
    /// </summary>
    public interface IOrgListClient
    {
        /// <summary>
        /// Returns a list of orga
        /// </summary>
        /// <returns></returns>
        public Task<OrgList> GetOrgList();
    }
}
