using System.Buffers;
using System.Diagnostics;
using System.Net;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Exceptions;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;

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
        private readonly IAltinn2Services _altinn2ServicesClient;
        private readonly IApplications _applicationsClient;
        private readonly IOrgListClient _orgList;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Creates a new instance of the <see cref="ResourceRegistryService"/> service.
        /// The ResourceRegistryService is responcible for business logic and implementations for working with the resource registry
        /// </summary>
        /// <param name="repository">Resource registry repository implementation for dependencies to its operations</param>
        /// <param name="policyRepository">Repository implementation for operations on policies</param>
        /// <param name="accessManagementClient">client to send data to AccessManagement</param>
        /// <param name="altinn2ServicesClient">Used to retrieve information from Altinn 2</param>
        /// <param name="applicationsClient">Used to retrieve information from Altinn Storage about Altinn 3 apps</param>
        /// <param name="orgList">Client to retrive orglist</param>
        /// <param name="memoryCache">Memorycache</param>
        public ResourceRegistryService(
            IResourceRegistryRepository repository, 
            IPolicyRepository policyRepository, 
            IAccessManagementClient accessManagementClient, 
            IAltinn2Services altinn2ServicesClient, 
            IApplications applicationsClient, 
            IOrgListClient orgList, 
            IMemoryCache memoryCache)
        {
            _repository = repository;
            _policyRepository = policyRepository;
            _accessManagementClient = accessManagementClient;
            _altinn2ServicesClient = altinn2ServicesClient;
            _applicationsClient = applicationsClient;
            _orgList = orgList;
            _memoryCache = memoryCache;
        }

        /// <inheritdoc/>
        public async Task CreateResource(ServiceResource serviceResource, CancellationToken cancellationToken = default)
        {
            bool result = await UpdateResourceInAccessManagement(serviceResource, cancellationToken);
            if (!result)
            {
                throw new AccessManagementUpdateException("Updating Access management failed");
            }

            await _repository.CreateResource(serviceResource, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateResource(ServiceResource serviceResource, CancellationToken cancellationToken = default)
        {
            bool result = await UpdateResourceInAccessManagement(serviceResource, cancellationToken);
            if (!result)
            {
                throw new AccessManagementUpdateException("Updating Access management failed");
            }

            await _repository.UpdateResource(serviceResource, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task Delete(string id, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteResource(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResource(string id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetResource(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch, CancellationToken cancellationToken = default)
        {
            return await _repository.Search(resourceSearch, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> StorePolicy(ServiceResource serviceResource, ReadOnlySequence<byte> policyContent, CancellationToken cancellationToken = default)
        {
            PolicyHelper.IsValidResourcePolicy(serviceResource, policyContent);
            Response<BlobContentInfo> response = await _policyRepository.WritePolicyAsync(serviceResource.Identifier, policyContent.AsStream(), cancellationToken);

            return response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
        }

        /// <inheritdoc/>
        public async Task<Stream> GetPolicy(string resourceId, CancellationToken cancellationToken = default)
        {
              return await _policyRepository.GetPolicyAsync(resourceId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> UpdateResourceInAccessManagement(ServiceResource serviceResource, CancellationToken cancellationToken = default)
        {
            AccessManagementResource convertedElement = new AccessManagementResource(serviceResource);
            List<AccessManagementResource> convertedElementList = convertedElement.ElementToList();
            HttpResponseMessage response = await _accessManagementClient.AddResourceToAccessManagement(convertedElementList, cancellationToken);

            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <inheritdoc />
        public async Task<List<ServiceResource>> GetResourceList(bool includeApps, bool includeAltinn2, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<List<ServiceResource>>>(3)
            {
                GetResourceListInner(cancellationToken),
            };

            if (includeApps || includeAltinn2)
            {
                var orgListTask = GetOrgList(cancellationToken);
                
                if (includeAltinn2)
                {
                    tasks.Add(GetAltinn2AvailableServices(orgListTask, cancellationToken));
                }
                
                if (includeApps)
                {
                    tasks.Add(GetAltinn3Applications(orgListTask, cancellationToken));
                }
            }

            var resourceLists = await Task.WhenAll(tasks);
            var resourcesCount = resourceLists.Sum(list => list.Count);
            var resources = new List<ServiceResource>(resourcesCount);
            foreach (var resourceList in resourceLists)
            { 
                resources.AddRange(resourceList);
            }

            return resources;

            async Task<List<ServiceResource>> GetResourceListInner(CancellationToken cancellationToken)
            {
                ResourceSearch resourceSearch = new ResourceSearch();
            
                List<ServiceResource> resources = await Search(resourceSearch, cancellationToken);

                foreach (ServiceResource resource in resources)
                {
                    resource.AuthorizationReference = new List<AuthorizationReferenceAttribute>
                    {
                        new AuthorizationReferenceAttribute() { Id = "urn:altinn:resource", Value = resource.Identifier }
                    };
                }

                return resources;
            }

            async Task<List<ServiceResource>> GetAltinn3Applications(Task<OrgList> orgListTask, CancellationToken cancellationToken = default)
            {
                ApplicationList applicationList = await _applicationsClient.GetApplicationList(cancellationToken);
                var orgList = await orgListTask;

                return applicationList.Applications.Select(application => MapApplicationToApplicationResource(application, orgList)).ToList();
            }

            async Task<List<ServiceResource>> GetAltinn2AvailableServices(Task<OrgList> orgListTask, CancellationToken cancellationToken = default)
            {
                var altin2Services = await Task.WhenAll(
                    _altinn2ServicesClient.AvailableServices(1044, cancellationToken),
                    _altinn2ServicesClient.AvailableServices(2068, cancellationToken),
                    _altinn2ServicesClient.AvailableServices(1033, cancellationToken));

                List<AvailableService> altinn2List1044 = altin2Services[0];
                List<AvailableService> altinn2List2068 = altin2Services[1];
                List<AvailableService> altinn2List1033 = altin2Services[2];
                var orgList = await orgListTask;

                var serviceResources = new List<ServiceResource>(altinn2List1044.Count);
                foreach (AvailableService service in altinn2List1044)
                {
                    string nntext = string.Empty;
                    string entext = string.Empty;

                    AvailableService service2068 = altinn2List2068.Find(r => r.ExternalServiceCode == service.ExternalServiceCode && r.ExternalServiceEditionCode == service.ExternalServiceEditionCode);
                    if (service2068 != null)
                    {
                        nntext = service2068.ServiceEditionVersionName;
                    }
                    
                    AvailableService service1033 = altinn2List1033.Find(r => r.ExternalServiceCode == service.ExternalServiceCode && r.ExternalServiceEditionCode == service.ExternalServiceEditionCode);
                    if (service1033 != null)
                    {
                        entext = service1033.ServiceEditionVersionName;
                    }

                    serviceResources.Add(MapAltinn2ServiceToServiceResource(service, orgList, entext, nntext));
                }

                return serviceResources;
            }

            ServiceResource MapAltinn2ServiceToServiceResource(AvailableService availableService, OrgList orgList, string entext, string nntext)
            {
                ServiceResource serviceResource = new ServiceResource();
                serviceResource.ResourceType = Enums.ResourceType.Altinn2Service;
                serviceResource.Title = new Dictionary<string, string>
                {
                    { "nb", availableService.ServiceEditionVersionName },
                    { "en", entext },
                    { "nn", nntext }
                };
                serviceResource.ResourceReferences = new List<ResourceReference>();
                serviceResource.Identifier = $"{ResourceConstants.SERVICE_ENGINE_RESOURCE_PREFIX}{availableService.ExternalServiceCode}_{availableService.ExternalServiceEditionCode.ToString()}";
                serviceResource.ResourceReferences.Add(new ResourceReference() { ReferenceType = Enums.ReferenceType.ServiceCode, Reference = availableService.ExternalServiceCode, ReferenceSource = Enums.ReferenceSource.Altinn2 });
                serviceResource.ResourceReferences.Add(new ResourceReference() { ReferenceType = Enums.ReferenceType.ServiceEditionCode, Reference = availableService.ExternalServiceEditionCode.ToString(), ReferenceSource = Enums.ReferenceSource.Altinn2 });
                serviceResource.AuthorizationReference = new List<AuthorizationReferenceAttribute>
                {
                    new AuthorizationReferenceAttribute() { Id = "urn:altinn:servicecode", Value = availableService.ExternalServiceCode },
                    new AuthorizationReferenceAttribute() { Id = "urn:altinn:serviceeditioncode", Value = availableService.ExternalServiceEditionCode.ToString() }
                };
                serviceResource.HasCompetentAuthority = new CompetentAuthority();
                serviceResource.HasCompetentAuthority.Orgcode = availableService.ServiceOwnerCode.ToLower();
                if (orgList.Orgs.TryGetValue(serviceResource.HasCompetentAuthority.Orgcode.ToLower(), out Org orgentity))
                {
                    serviceResource.HasCompetentAuthority.Organization = orgentity.Orgnr;
                    serviceResource.HasCompetentAuthority.Name = orgentity.Name;
                }

                return serviceResource;
            }

            ServiceResource MapApplicationToApplicationResource(Application application, OrgList orgList)
            {
                ServiceResource service = new ServiceResource();
                service.Title = application.Title;
                service.Identifier = $"{ResourceConstants.APPLICATION_RESOURCE_PREFIX}{application.Org}_{application.Id.Substring(application.Id.IndexOf("/") + 1)}";
                service.ResourceType = Enums.ResourceType.AltinnApp;
                service.ResourceReferences = new List<ResourceReference>
                {
                    new ResourceReference() { ReferenceSource = Enums.ReferenceSource.Altinn3, ReferenceType = Enums.ReferenceType.ApplicationId, Reference = application.Id }
                };
                service.AuthorizationReference = new List<AuthorizationReferenceAttribute>
                {
                    new AuthorizationReferenceAttribute() { Id = "urn:altinn:org", Value = application.Org },
                    new AuthorizationReferenceAttribute() { Id = "urn:altinn:app", Value = application.Id.Substring(application.Id.IndexOf("/") + 1) }
                };
                service.HasCompetentAuthority = new CompetentAuthority();
                service.HasCompetentAuthority.Orgcode = application.Org.ToLower();
                if (orgList.Orgs.TryGetValue(service.HasCompetentAuthority.Orgcode, out Org orgentity))
                {
                    service.HasCompetentAuthority.Organization = orgentity.Orgnr;
                    service.HasCompetentAuthority.Name = orgentity.Name;
                }

                return service;
            }

            async Task<OrgList> GetOrgList(CancellationToken cancellationToken = default)
            {
                string cacheKey = "fullorglist";

                if (!_memoryCache.TryGetValue(cacheKey, out OrgList orgList))
                {
                    orgList = await _orgList.GetOrgList(cancellationToken);
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetPriority(CacheItemPriority.High)
                        .SetAbsoluteExpiration(new TimeSpan(0, 3600, 0));

                    if (orgList != null)
                    {
                        _memoryCache.Set(cacheKey, orgList, cacheEntryOptions);
                    }
                }

                return orgList;
            }
        }
    }
}
