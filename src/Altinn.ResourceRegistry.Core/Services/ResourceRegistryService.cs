using System.Net;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Exceptions;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Service implementation for operations on the resource registry
    /// </summary>
    public class ResourceRegistryService : IResourceRegistry
    {
        private readonly IResourceRegistryRepository _repository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IAccessManagementClient _accessManagementClient;

        /// <summary>
        /// Creates a new instance of the <see cref="ResourceRegistryService"/> service.
        /// The ResourceRegistryService is responcible for business logic and implementations for working with the resource registry
        /// </summary>
        /// <param name="repository">Resource registry repository implementation for dependencies to its operations</param>
        /// <param name="policyRepository">Repository implementation for operations on policies</param>
        /// <param name="logger">Logger</param>
        /// <param name="accessManagementClient">client to send data to AccessManagement</param>
        public ResourceRegistryService(IResourceRegistryRepository repository, IPolicyRepository policyRepository, ILogger<ResourceRegistryService> logger, IAccessManagementClient accessManagementClient)
        {
            _repository = repository;
            _policyRepository = policyRepository;
            _accessManagementClient = accessManagementClient;
        }

        /// <inheritdoc/>
        public async Task CreateResource(ServiceResource serviceResource)
        {
            bool result = await UpdateResourceInAccessManagement(serviceResource);
            if (!result)
            {
                throw new AccessManagementUpdateException("Updating Access management failed");
            }

            await _repository.CreateResource(serviceResource);
        }

        /// <inheritdoc/>
        public async Task UpdateResource(ServiceResource serviceResource)
        {
            bool result = await UpdateResourceInAccessManagement(serviceResource);
            if (!result)
            {
                throw new AccessManagementUpdateException("Updating Access management failed");
            }

            await _repository.UpdateResource(serviceResource);
        }

        /// <inheritdoc/>
        public async Task Delete(string id)
        {
            await _repository.DeleteResource(id);
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResource(string id)
        {
            return await _repository.GetResource(id);
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch)
        {
            return await _repository.Search(resourceSearch);
        }

        /// <inheritdoc/>
        public async Task<bool> StorePolicy(ServiceResource serviceResource, Stream fileStream)
        {
            PolicyHelper.IsValidResourcePolicy(serviceResource, fileStream);

            string filePath = $"{serviceResource.Identifier.AsFilePath()}/resourcepolicy.xml";
            Response<BlobContentInfo> response = await _policyRepository.WritePolicyAsync(filePath, fileStream);

            return response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
        }

        /// <inheritdoc/>
        public async Task<Stream> GetPolicy(string resourceId)
        {
              return await _policyRepository.GetPolicyAsync(resourceId);
        }

        /// <inheritdoc />
        public async Task<bool> UpdateResourceInAccessManagement(ServiceResource serviceResource)
        {
            AccessManagementResource convertedElement = new AccessManagementResource(serviceResource);
            List<AccessManagementResource> convertedElementList = convertedElement.ElementToList();
            HttpResponseMessage response = await _accessManagementClient.AddResourceToAccessManagement(convertedElementList);

            return response.StatusCode == HttpStatusCode.Created;
        }
    }
}
