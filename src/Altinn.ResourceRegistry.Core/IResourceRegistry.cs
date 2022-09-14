using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core
{
    public interface IResourceRegistry
    {
        Task<ServiceResource> GetResource(string id);

        Task CreateResource(ServiceResource serviceResource);

        Task UpdateResource(ServiceResource serviceResource);

        Task Delete(string id);

        Task<List<ServiceResource>> Search(ResourceSearch resourceSearch);
    }
}
