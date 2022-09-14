using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core
{
    public class ResourceRegistryService : IResourceRegistry
    {
        private IResourceRegistryRepository _repository;

        public ResourceRegistryService(IResourceRegistryRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateResource(ServiceResource serviceResource)
        {
            await _repository.CreateResource(serviceResource);
        }

        public async Task Delete(string id)
        {
            await _repository.DeleteResource(id);
        }

        public async Task<ServiceResource> GetResource(string id)
        {
            return await _repository.GetResource(id);
        }

        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch)
        {
            return await _repository.Search(resourceSearch);
        }

        public async Task UpdateResource(ServiceResource serviceResource)
        {
            await _repository.UpdateResource(serviceResource);
        }
    }
}
