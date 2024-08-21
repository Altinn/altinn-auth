using System.Buffers;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for the ResourceRegistryService implementation
    /// </summary>
    public interface IResourceRegistry
    {
        /// <summary>
        /// Gets a single resource by its resource identifier if it exists in the resource registry
        /// </summary>
        /// <param name="id">The resource identifier to retrieve</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>ServiceResource</returns>
        Task<ServiceResource> GetResource(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the full list of 
        /// </summary>
        /// <param name="includeApps">Wheather or not to include apps</param>
        /// <param name="includeAltinn2">Wheather or not to include altinn 2 resources</param>
        /// <param name="includeExpired">Defines if expired Altinn 2 serviices should be include</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<List<ServiceResource>> GetResourceList(bool includeApps, bool includeAltinn2, bool includeExpired,  CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="serviceResource">Service resource model to create in the resource registry</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The result of the operation</returns>
        Task CreateResource(ServiceResource serviceResource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="serviceResource">Service resource model for update in the resource registry</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The result of the operation</returns>
        Task UpdateResource(ServiceResource serviceResource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a resource from the resource registry
        /// </summary>
        /// <param name="id">The resource identifier to delete</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        Task Delete(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Allows for searching for resources in the resource registry
        /// </summary>
        /// <param name="resourceSearch">The search model defining the search filter criterias</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A list of service resources found to match the search criterias</returns>
        Task<List<ServiceResource>> Search(ResourceSearch resourceSearch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Allows for searching for resources in the resource registry
        /// </summary>
        /// <param name="resourceSearch">The search model defining the search filter criterias</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A list of service resources found to match the search criterias</returns>
        Task<List<ServiceResource>> GetSearchResults(ResourceSearch resourceSearch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Allows for storing a policy xacml policy for the resource
        /// </summary>
        /// <param name="serviceResource">The resource</param>
        /// <param name="policyContent">The file stream to the policy file</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>Bool if storing the policy was successfull</returns>
        Task<bool> StorePolicy(ServiceResource serviceResource, ReadOnlySequence<byte> policyContent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the policy for a service resource
        /// </summary>
        /// <param name="resourceId">The resource id</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The policy as stream</returns>
        Task<Stream> GetPolicy(string resourceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates Access management with changes in recource registry
        /// </summary>
        /// <param name="serviceResource">The resource to add to access management</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns><see langword="true"/> if the update succeeded, otherwise <see langword="false"/></returns>
        Task<bool> UpdateResourceInAccessManagement(ServiceResource serviceResource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list over resources for what each subject has access to
        /// </summary>
        /// <param name="subjects">List of subjects</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>Resources for given subjects</returns>
        Task<List<SubjectResources>> FindResourcesForSubjects(List<string> subjects, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list of subjects in a policy for a resource
        /// </summary>
        /// <param name="resources">List of resource attributes</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>List of subjects for a given resource</returns>
        Task<List<ResourceSubjects>> FindSubjectsForResources(List<string> resources, CancellationToken cancellationToken = default);

        /// <summary>
        /// Based on a service resource the resource subjects will be reloaded on to database. 
        /// Created to support migrated apps and resource that was migrated before ResourceSubjecst whent in to production
        /// </summary>
        /// <param name="serviceResource">The service resource</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        Task UpdateResourceSubjectsFromResourcePolicy(ServiceResource serviceResource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Based on org and app loads app policy from policy storage and updates resource subjects for it.
        /// </summary>
        /// <param name="org">The organization</param>
        /// <param name="app">The app</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        Task UpdateResourceSubjectsFromAppPolicy(string org, string app, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list of resource/subject pairs (including deleted) that has been updated since lastUpdated
        /// </summary>
        /// <param name="lastUpdated">The timestamp from which to return updated entries</param>
        /// <param name="limit">The maximum number of entries to return</param>
        /// <param name="skipPast">Optional ResourceUrn,SubjectUrn pair to skip past if "since" value matches multiple rows</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>List of resource/subject pairs updated since lastUpdated</returns>
        Task<List<UpdatedResourceSubject>> FindUpdatedResourceSubjects(DateTimeOffset lastUpdated, int limit, (Uri ResourceUrn, Uri SubjectUrn)? skipPast = null, CancellationToken cancellationToken = default);
    }
}
