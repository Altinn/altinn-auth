using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class AccessManagementMock : IAccessManagementClient
    {
        public Task<HttpResponseMessage> AddResourceToAccessManagement(List<AccessManagementResource> resources, CancellationToken cancellationToken)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = JsonContent.Create(resources)
            };

            return Task.FromResult(responseMessage);
        }
    }
}
