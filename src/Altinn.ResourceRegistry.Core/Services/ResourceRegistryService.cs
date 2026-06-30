using System.Buffers;
using System.Net;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Exceptions;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
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
            IApplications applicationsClient,
            IServiceOwnerService serviceOwnerService)
        {
            _repository = repository;
            _policyRepository = policyRepository;
            _accessManagementClient = accessManagementClient;
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
        public async Task<ServiceResource> GetResource(string id, int? versionId, CancellationToken cancellationToken = default)
        {
            return await _repository.GetResource(id, versionId,  cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<CompetentAuthorityReference>> GetResourceOwner(string id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetResourceOwner(id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch, bool includeAllVersions, CancellationToken cancellationToken = default)
        {
            return await _repository.Search(resourceSearch, includeAllVersions, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> GetSearchResults(ResourceSearch resourceSearch, CancellationToken cancellationToken = default)
        {
            List<ServiceResource> resourceList = await GetResourceList(includeApps: false, includeExpired: false, includeMigratedApps: false, includeAllVersions: true, cancellationToken);
            return ServiceResourceHelper.GetSearchResultsFromResourceList(resourceList, resourceSearch);
        }

        /// <inheritdoc/>
        public async Task<bool> StorePolicy(ServiceResource serviceResource, ReadOnlySequence<byte> policyContent, CancellationToken cancellationToken = default)
        {
            XacmlPolicy policy = PolicyHelper.ParsePolicy(policyContent);
            PolicyHelper.EnsureValidPolicy(serviceResource, policy);
            
            Response<BlobContentInfo> response;
            
            // App resources should be stored in the metadata container at org/app/policy.xml
            if (serviceResource.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = serviceResource.Identifier.Split('_', 3);
                if (parts.Length == 3)
                {
                    string org = parts[1];
                    string app = parts[2];
                    response = await _policyRepository.WriteAppPolicyAsync(org, app, policyContent.AsStream(), cancellationToken);
                }
                else
                {
                    // Fallback to standard location if identifier format is unexpected
                    response = await _policyRepository.WritePolicyAsync(serviceResource.Identifier, policyContent.AsStream(), cancellationToken);
                }
            }
            else
            {
                // Standard resources stored at resourceId/resourcepolicy.xml
                response = await _policyRepository.WritePolicyAsync(serviceResource.Identifier, policyContent.AsStream(), cancellationToken);
            }
            
            IDictionary<string, ICollection<string>> subjectAttributes = policy.GetAttributeDictionaryByCategory(XacmlConstants.MatchAttributeCategory.Subject);
            ResourceSubjects resourceSubjects = GetResourceSubjects(serviceResource, subjectAttributes);
            await _repository.SetResourceSubjects(resourceSubjects, CancellationToken.None);
            return response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
        }

        /// <inheritdoc/>
        public async Task UpdateResourceSubjectsFromResourcePolicy(ServiceResource serviceResource, CancellationToken cancellationToken = default)
        {
            XacmlPolicy? policy = await GetXacmlPolicy(serviceResource.Identifier, cancellationToken);
            if (policy == null)
            {
                throw new InvalidOperationException($"Policy not found for resource {serviceResource.Identifier}");
            }

            IDictionary<string, ICollection<string>> subjectAttributes = policy.GetAttributeDictionaryByCategory(XacmlConstants.MatchAttributeCategory.Subject);
            ResourceSubjects resourceSubjects = GetResourceSubjects(serviceResource, subjectAttributes);
            await _repository.SetResourceSubjects(resourceSubjects, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateResourceSubjectsFromAppPolicy(string org, string app, CancellationToken cancellationToken = default)
        {
            Stream? policyContent = await _policyRepository.GetAppPolicyAsync(org, app, cancellationToken);
            if (policyContent == null)
            {
                throw new InvalidOperationException($"Policy not found for app {org}/{app}");
            }

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
        public async Task<List<ServiceResource>> GetResourceList(bool includeApps, bool includeExpired, bool includeMigratedApps, bool includeAllVersions = false, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<List<ServiceResource>>>(2)
            {
                GetResourceListInner(cancellationToken),
            };

            if (includeApps)
            {
                tasks.Add(GetAltinn3Applications(includeMigratedApps, cancellationToken));
            }

            var resourceLists = await Task.WhenAll(tasks);
            
            // Get Resource Registry resources (always first in the list)
            var registryResources = resourceLists[0];
            
            // Build a HashSet of app identifiers that exist in Resource Registry
            var registryAppIdentifiers = new HashSet<string>(
                registryResources
                    .Where(r => r.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX, StringComparison.OrdinalIgnoreCase))
                    .Select(r => r.Identifier),
                StringComparer.OrdinalIgnoreCase);
            
            // Combine all resources, but filter out Storage apps that are already in Resource Registry
            var resources = new List<ServiceResource>(registryResources);
            
            for (int i = 1; i < resourceLists.Length; i++)
            {
                var resourceList = resourceLists[i];
                foreach (var resource in resourceList)
                {
                    // Skip Storage apps that are already published to Resource Registry
                    if (resource.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX, StringComparison.OrdinalIgnoreCase) 
                        && registryAppIdentifiers.Contains(resource.Identifier))
                    {
                        continue; // Skip this duplicate
                    }
                    
                    resources.Add(resource);
                }
            }

            return resources;

            async Task<List<ServiceResource>> GetResourceListInner(CancellationToken cancellationToken)
            {
                ResourceSearch resourceSearch = new ResourceSearch();

                List<ServiceResource> resources = await Search(resourceSearch, includeAllVersions, cancellationToken);

                if (!includeMigratedApps)
                {
                    resources = resources
                        .Where(resource => !IsMigratedApplicationResource(resource))
                        .ToList();
                }

                foreach (ServiceResource resource in resources)
                {
                    // For app resources, use org/app format like Storage apps do
                    if (resource.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX, StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = resource.Identifier.Split('_', 3);
                        if (parts.Length == 3)
                        {
                            string org = parts[1];
                            string app = parts[2];
                            resource.AuthorizationReference = new List<AuthorizationReferenceAttribute>
                            {
                                new AuthorizationReferenceAttribute() { Id = "urn:altinn:org", Value = org },
                                new AuthorizationReferenceAttribute() { Id = "urn:altinn:app", Value = app }
                            };
                        }
                        else
                        {
                            // Fallback if identifier format is unexpected
                            resource.AuthorizationReference = new List<AuthorizationReferenceAttribute>
                            {
                                new AuthorizationReferenceAttribute() { Id = "urn:altinn:resource", Value = resource.Identifier }
                            };
                        }
                    }
                    else
                    {
                        // Non-app resources use resource format
                        resource.AuthorizationReference = new List<AuthorizationReferenceAttribute>
                        {
                            new AuthorizationReferenceAttribute() { Id = "urn:altinn:resource", Value = resource.Identifier }
                        };
                    }
                }

                return resources;

                static bool IsMigratedApplicationResource(ServiceResource resource)
                {
                    return resource.ResourceType == Enums.ResourceType.MigratedApp;
                }
            }

            async Task<List<ServiceResource>> GetAltinn3Applications(bool includeMigratedApps, CancellationToken cancellationToken = default)
            {
                ApplicationList applicationList = await _applicationsClient.GetApplicationList(includeMigratedApps, cancellationToken);
                var serviceOwners = await _serviceOwnerService.GetServiceOwners(cancellationToken);

                return applicationList.Applications.Select(application => MapApplicationToApplicationResource(application, serviceOwners)).ToList();
            }

            ServiceResource MapApplicationToApplicationResource(Application application, ServiceOwnerLookup serviceOwners)
            {
                ServiceResource service = new ServiceResource();
                service.Title = application.Title;
                service.Identifier = $"{ResourceConstants.APPLICATION_RESOURCE_PREFIX}{application.Org}_{application.Id.Substring(application.Id.IndexOf("/") + 1)}";
                if (service.Identifier.Contains("_a2-") || service.Identifier.Contains("_a1-"))
                {
                    service.ResourceType = Enums.ResourceType.MigratedApp;
                }
                else
                {
                    service.ResourceType = Enums.ResourceType.AltinnApp;
                }

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

                if (application.Id.StartsWith("a1-"))
                {
                    service.Delegable = false;
                    service.Visible = false;
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

        /// <inheritdoc/>
        public async Task<List<Right>> GetPolicyRightsV2(string resourceId, bool includeServiceOwnerRights, bool includeAppRights, CancellationToken cancellationToken = default)
        {
            XacmlPolicy policy = await GetXacmlPolicy(resourceId, cancellationToken);
            if (policy == null)
            {
                return null;
            }
            
            // Decompose policy into resource/tasks
            List<Right> rights = DelegationCheckHelper.DecomposePolicy(policy, resourceId, includeServiceOwnerRights, includeAppRights);

            return rights;
        }

        private async Task<XacmlPolicy?> GetXacmlPolicy(string resourceIdentifer, CancellationToken cancellationToken)
        {
            Stream? policyContent = null;
            if (resourceIdentifer.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX))
            {
                string[] idParts = resourceIdentifer.Split('_', 3);

                // Scenario for app imported in to resource registry
                if (idParts.Length == 3)
                {
                    string org = idParts[1];
                    string app = idParts[2];
                    policyContent = await _policyRepository.GetAppPolicyAsync(org, app, cancellationToken);
                }
            }
            else
            {
                policyContent = await _policyRepository.GetPolicyAsync(resourceIdentifer, cancellationToken);
            }

            if (policyContent == null)
            {
                return null;
            }

            policyContent.Position = 0;
            XacmlPolicy policy = await PolicyHelper.ParsePolicy(policyContent);
            return policy;
        }
    }
}
