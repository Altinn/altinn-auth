using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Clients.Interfaces
{
    /// <summary>
    /// interface for Access Management Client
    /// </summary>
    public interface IAccessManagementClient
    {
        /// <summary>
        /// Adds a resource to Access Management
        /// </summary>
        /// <param name="resources">input to store in Accerss Management</param>
        /// <returns>The http response</returns>
        Task<HttpResponseMessage> AddResourceToAccessManagement(List<AccessManagementResource> resources);
    }
}
