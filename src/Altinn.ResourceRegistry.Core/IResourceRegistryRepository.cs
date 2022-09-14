using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;

namespace Altinn.ResourceRegistry.Core
{
    public interface IResourceRegistryRepository
    {
        Task<ServiceResource> GetResource(string id);

        Task<ServiceResource> DeleteResource(string id);

        Task<ServiceResource> UpdateResource(ServiceResource resource);

        Task<ServiceResource> CreateResource(ServiceResource resource);

        Task<List<ServiceResource>> Search(ResourceSearch resourceSearch);
    }
}