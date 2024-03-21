using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core
{
    /// <summary>
    /// Interface for the postgre repository for resource registry
    /// </summary>
    public interface IResourceRegistryRepository
    {
        /// <summary>
        /// Gets a single resource by its resource identifier if it exists in the resource registry
        /// </summary>
        /// <param name="id">The resource identifier to retrieve</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling the async process.</param>
        /// <returns>ServiceResource</returns>
        Task<ServiceResource> GetResource(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a resource from the resource registry
        /// </summary>
        /// <param name="id">The resource identifier to delete</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling the async process.</param>
        Task<ServiceResource> DeleteResource(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="resource">Service resource model for update in the resource registry</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling the async process.</param>
        /// <returns>The result of the operation</returns>
        Task<ServiceResource> UpdateResource(ServiceResource resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="resource">Service resource model to create in the resource registry</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling the async process.</param>
        /// <returns>The result of the operation</returns>
        Task<ServiceResource> CreateResource(ServiceResource resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Allows for searching for resources in the resource registry
        /// </summary>
        /// <param name="resourceSearch">The search model defining the search filter criterias</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> for cancelling the async process.</param>
        /// <returns>A list of service resources found to match the search criterias</returns>
        Task<List<ServiceResource>> Search(ResourceSearch resourceSearch, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list over resources for what each subject has access to
        /// </summary>
        /// <param name="subjects">List of subjects</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<List<SubjectResources>> FindResourcesForSubjects(IEnumerable<string> subjects, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a list of subjects in a policy for a resource
        /// </summary>
        /// <param name="resources">List of resource attributes</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<List<ResourceSubjects>> FindSubjectsForResources(IEnumerable<string> resources, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resett subjects for a given resource
        /// </summary>
        /// <param name="resourceSubjects">The resourceSubjects with resource and list of subjects</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task SetResourceSubjects(ResourceSubjects resourceSubjects, CancellationToken cancellationToken = default);
    }
}
