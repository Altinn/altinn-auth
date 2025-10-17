using System.Buffers;
using System.Collections.Generic;
using System.Net;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Exceptions;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.ServiceOwners;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Azure;
using Azure.Storage.Blobs.Models;
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
        private readonly IServiceOwnerService _serviceOwnerService;

        /// <summary>
        /// Creates a new instance of the <see cref="ResourceRegistryService"/> service.
        /// The ResourceRegistryService is responsible for business logic and implementations for working with the resource registry
        /// </summary>
        public ResourceRegistryService(
            IResourceRegistryRepository repository,
            IPolicyRepository policyRepository,
            IAccessManagementClient accessManagementClient,
            IAltinn2Services altinn2ServicesClient,
            IApplications applicationsClient,
            IServiceOwnerService serviceOwnerService)
        {
            _repository = repository;
            _policyRepository = policyRepository;
            _accessManagementClient = accessManagementClient;
            _altinn2ServicesClient = altinn2ServicesClient;
            _applicationsClient = applicationsClient;
            _serviceOwnerService = serviceOwnerService;
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
            bool deletePolicyResult = await _policyRepository.TryDeletePolicyAsync(id, cancellationToken);
            if (!deletePolicyResult)
            {
                throw new AccessManagementUpdateException($"Deleting policy for resource {id} failed");
            }

            await _repository.DeleteResource(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResource(string id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetResource(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<CompetentAuthorityReference>> GetResourceOwner(string id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetResourceOwner(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch, CancellationToken cancellationToken = default)
        {
            return await _repository.Search(resourceSearch, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> GetSearchResults(ResourceSearch resourceSearch, CancellationToken cancellationToken = default)
        {
            List<ServiceResource> resourceList = await GetResourceList(includeApps: false, includeAltinn2: false, includeExpired: false, includeMigratedApps: false, cancellationToken);
            return ServiceResourceHelper.GetSearchResultsFromResourceList(resourceList, resourceSearch);
        }

        /// <inheritdoc/>
        public async Task<bool> StorePolicy(ServiceResource serviceResource, ReadOnlySequence<byte> policyContent, CancellationToken cancellationToken = default)
        {
            XacmlPolicy policy = PolicyHelper.ParsePolicy(policyContent);
            PolicyHelper.EnsureValidPolicy(serviceResource, policy);
            Response<BlobContentInfo> response = await _policyRepository.WritePolicyAsync(serviceResource.Identifier, policyContent.AsStream(), cancellationToken);
            IDictionary<string, ICollection<string>> subjectAttributes = policy.GetAttributeDictionaryByCategory(XacmlConstants.MatchAttributeCategory.Subject);
            ResourceSubjects resourceSubjects = GetResourceSubjects(serviceResource, subjectAttributes);
            await _repository.SetResourceSubjects(resourceSubjects, CancellationToken.None);
            return response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
        }

        /// <inheritdoc/>
        public async Task UpdateResourceSubjectsFromResourcePolicy(ServiceResource serviceResource, CancellationToken cancellationToken = default)
        {
            XacmlPolicy policy = await GetXacmlPolicy(serviceResource.Identifier, cancellationToken);
            IDictionary<string, ICollection<string>> subjectAttributes = policy.GetAttributeDictionaryByCategory(XacmlConstants.MatchAttributeCategory.Subject);
            ResourceSubjects resourceSubjects = GetResourceSubjects(serviceResource, subjectAttributes);
            await _repository.SetResourceSubjects(resourceSubjects, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateResourceSubjectsFromAppPolicy(string org, string app, CancellationToken cancellationToken = default)
        {
            Stream policyContent = await _policyRepository.GetAppPolicyAsync(org, app, cancellationToken);
            policyContent.Position = 0;
            XacmlPolicy policy = await PolicyHelper.ParsePolicy(policyContent);
            IDictionary<string, ICollection<string>> subjectAttributes = policy.GetAttributeDictionaryByCategory(XacmlConstants.MatchAttributeCategory.Subject);
            ServiceResource virtualAppResource = new ServiceResource()
            {
                Identifier = $"app_{org.ToLower()}_{app.ToLower()}".ToLower(),
                HasCompetentAuthority = new CompetentAuthority()
                {
                    Orgcode = org.ToLower()
                }
            };
            ResourceSubjects resourceSubjects = GetResourceSubjects(virtualAppResource, subjectAttributes);
            await _repository.SetResourceSubjects(resourceSubjects, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Stream> GetPolicy(string resourceId, CancellationToken cancellationToken = default)
        {
            return await _policyRepository.GetPolicyAsync(resourceId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Stream> GetAppPolicy(string org, string app,CancellationToken cancellationToken = default)
        {
            return await _policyRepository.GetAppPolicyAsync(org, app, cancellationToken);
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
        public async Task<List<ServiceResource>> GetResourceList(bool includeApps, bool includeAltinn2, bool includeExpired, bool includeMigratedApps, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<List<ServiceResource>>>(3)
            {
                GetResourceListInner(cancellationToken),
            };

            if (includeApps || includeAltinn2)
            {
                if (includeAltinn2)
                {
                    tasks.Add(GetAltinn2AvailableServices(includeExpired: false, cancellationToken));
                }

                if (includeApps)
                {
                    tasks.Add(GetAltinn3Applications(includeMigratedApps, cancellationToken));
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

            async Task<List<ServiceResource>> GetAltinn3Applications(bool includeMigratedApps, CancellationToken cancellationToken = default)
            {
                ApplicationList applicationList = await _applicationsClient.GetApplicationList(includeMigratedApps, cancellationToken);
                var serviceOwners = await _serviceOwnerService.GetServiceOwners(cancellationToken);

                return applicationList.Applications.Select(application => MapApplicationToApplicationResource(application, serviceOwners)).ToList();
            }

            async Task<List<ServiceResource>> GetAltinn2AvailableServices(bool includeExpired, CancellationToken cancellationToken = default)
            {
                var altin2Services = await Task.WhenAll(
                    _altinn2ServicesClient.AvailableServices(1044, includeExpired, cancellationToken),
                    _altinn2ServicesClient.AvailableServices(2068, includeExpired, cancellationToken),
                    _altinn2ServicesClient.AvailableServices(1033, includeExpired, cancellationToken));

                List<AvailableService> altinn2List1044 = altin2Services[0];
                List<AvailableService> altinn2List2068 = altin2Services[1];
                List<AvailableService> altinn2List1033 = altin2Services[2];
                var serviceOwners = await _serviceOwnerService.GetServiceOwners(cancellationToken);

                var serviceResources = new List<ServiceResource>(altinn2List1044.Count);
                foreach (AvailableService service in altinn2List1044)
                {
                    string nntext = string.Empty;
                    string entext = string.Empty;

                    string nndelegationDescription = string.Empty;
                    string endelegationDescription = string.Empty;

                    AvailableService service2068 = altinn2List2068.Find(r => r.ExternalServiceCode == service.ExternalServiceCode && r.ExternalServiceEditionCode == service.ExternalServiceEditionCode);
                    if (service2068 != null)
                    {
                        nntext = service2068.ServiceEditionVersionName;
                        nndelegationDescription = service2068.DelegationDescription;
                    }

                    AvailableService service1033 = altinn2List1033.Find(r => r.ExternalServiceCode == service.ExternalServiceCode && r.ExternalServiceEditionCode == service.ExternalServiceEditionCode);
                    if (service1033 != null)
                    {
                        entext = service1033.ServiceEditionVersionName;
                        endelegationDescription = service1033.DelegationDescription;
                    }

                    serviceResources.Add(MapAltinn2ServiceToServiceResource(service, serviceOwners, entext, nntext, endelegationDescription, nndelegationDescription));
                }

                return serviceResources;
            }

            ServiceResource MapAltinn2ServiceToServiceResource(AvailableService availableService, ServiceOwnerLookup serviceOwners, string entext, string nntext, string endelegationDescription, string nndelegationDescription)
            {
                ServiceResource serviceResource = new ServiceResource();
                serviceResource.ResourceType = Enums.ResourceType.Altinn2Service;
                serviceResource.Title = new Dictionary<string, string>
                {
                    { "nb", availableService.ServiceEditionVersionName },
                    { "en", entext },
                    { "nn", nntext }
                };

                if (!string.IsNullOrEmpty(availableService.DelegationDescription)
                    || !string.IsNullOrEmpty(endelegationDescription)
                    || !string.IsNullOrEmpty(nndelegationDescription))
                {
                    serviceResource.RightDescription = new Dictionary<string, string>();
                }

                if (!string.IsNullOrEmpty(availableService.DelegationDescription))
                {
                    serviceResource.RightDescription.Add("nb", availableService.DelegationDescription);
                }

                if (!string.IsNullOrEmpty(endelegationDescription))
                {
                    serviceResource.RightDescription.Add("en", endelegationDescription);
                }

                if (!string.IsNullOrEmpty(endelegationDescription))
                {
                    serviceResource.RightDescription.Add("nn", nndelegationDescription);
                }

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
                if (serviceOwners.TryGet(serviceResource.HasCompetentAuthority.Orgcode.ToLower(), out var orgentity))
                {
                    serviceResource.HasCompetentAuthority.Organization = orgentity.OrganizationNumber.ToString();
                    serviceResource.HasCompetentAuthority.Name = orgentity.Name;
                }

                return serviceResource;
            }

            ServiceResource MapApplicationToApplicationResource(Application application, ServiceOwnerLookup serviceOwners)
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
                if (serviceOwners.TryGet(service.HasCompetentAuthority.Orgcode, out var orgentity))
                {
                    service.HasCompetentAuthority.Organization = orgentity.OrganizationNumber.ToString();
                    service.HasCompetentAuthority.Name = orgentity.Name;
                }

                return service;
            }
        }

        /// <inheritdoc/>
        public async Task<List<SubjectResources>> FindResourcesForSubjects(List<string> subjects, CancellationToken cancellationToken = default)
        {
            return await _repository.FindResourcesForSubjects(subjects, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<ResourceSubjects>> FindSubjectsForResources(List<string> resources, CancellationToken cancellationToken = default)
        {
            return await _repository.FindSubjectsForResources(resources, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<UpdatedResourceSubject>> FindUpdatedResourceSubjects(DateTimeOffset lastUpdated, int limit, (Uri ResourceUrn, Uri SubjectUrn)? skipPast = null, CancellationToken cancellationToken = default)
        {
            return await _repository.FindUpdatedResourceSubjects(lastUpdated, limit, skipPast, cancellationToken);
        }

        private static ResourceSubjects GetResourceSubjects(ServiceResource resource, IDictionary<string, ICollection<string>> subjectAttributes)
        {
            AttributeMatchV2 resourceAttribute = new AttributeMatchV2
            {
                Type = AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute,
                Value = resource.Identifier,
                Urn = $"{AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute}:{resource.Identifier}"
            };

            string resourceOwner = string.Empty;
            if (resource.HasCompetentAuthority?.Orgcode != null)
            {
                resourceOwner = $"{AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute}:{resource.HasCompetentAuthority.Orgcode}";
            }

            List<AttributeMatchV2> subjectAttributeMatches = new List<AttributeMatchV2>();
            foreach (KeyValuePair<string, ICollection<string>> kvp in subjectAttributes)
            {
                foreach (string subjectAttributeValue in kvp.Value)
                {
                    AttributeMatchV2 subjectMatch = new AttributeMatchV2
                    {
                        Type = kvp.Key,
                        Value = subjectAttributeValue.ToLower(),
                        Urn = $"{kvp.Key}:{subjectAttributeValue.ToLower()}"
                    };

                    if (!subjectAttributeMatches.Exists(r => r.Urn.Equals(subjectMatch.Urn)))
                    {
                        subjectAttributeMatches.Add(subjectMatch);
                    }
                }
            }

            return new ResourceSubjects
            {
                Resource = resourceAttribute,
                Subjects = subjectAttributeMatches,
                ResourceOwner = resourceOwner
            };
        }

        /// <inheritdoc/>
        public async Task<List<PolicyRule>> GetFlattenPolicyRules(string resourceId, CancellationToken cancellationToken = default)
        {
            XacmlPolicy policy = await GetXacmlPolicy(resourceId, cancellationToken);
            if (policy == null)
            {
                return null;
            }

            List<PolicyRule> policyRules = PolicyHelper.ConvertToPolicyRules(policy);
            return policyRules;
        }

        /// <inheritdoc/>
        public async Task<List<PolicyRight>> GetPolicyRights(string resourceId, CancellationToken cancellationToken = default)
        {
            XacmlPolicy policy = await GetXacmlPolicy(resourceId, cancellationToken);
            if (policy == null)
            {
                return null;
            }

            List<PolicyRight> policyResourceActions = PolicyHelper.ConvertToPolicyRight(policy);
            return policyResourceActions;
        }

        private async Task<XacmlPolicy> GetXacmlPolicy(string resourceIdentifer, CancellationToken cancellationToken)
        {
            Stream policyContent = null;
            if (resourceIdentifer.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX))
            {
                string[] idParts = resourceIdentifer.Split('_');

                // Scenario for app imported in to resource registry
                if (idParts.Length == 3)
                {
                    string org = idParts[1];
                    string app = idParts[2];
                    policyContent = await _policyRepository.GetAppPolicyAsync(org, app, cancellationToken);
                    policyContent.Position = 0;
                }
            }
            else
            {
                policyContent = await _policyRepository.GetPolicyAsync(resourceIdentifer, cancellationToken);
                policyContent.Position = 0;
            }

            XacmlPolicy policy = await PolicyHelper.ParsePolicy(policyContent);
            return policy;
        }
    }
}
