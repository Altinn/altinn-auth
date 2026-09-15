using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts
{
    public interface IServiceOwnerConnectionService
    {
        /// <summary>
        /// Allows service owners to add packages to an assignment connection two entities. If assignment does not exist , it will be created.
        /// This is used when a service owner wants to delegate access to a package they own between defined entities given by input, 
        /// </summary>
        /// <param name="fromId">The ID of the entity from which the package is being delegated.</param>
        /// <param name="toId">The ID of the entity to which the package is being delegated.</param>
        /// <param name="packageId">The ID of the package being added.</param>
        /// <param name="configureConnection">An optional action to configure connection options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>It returns a result indicating whether the operation was successful and includes the details of the assignment package if it was added successfully.</returns>
        Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Guid packageId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Allows service owners to revoke packages to an assignment connecting two entities,
        /// if assignment is empty after revoke it will be revoked as well.
        /// Only if the assgnment of the packake was done by the service owner it will be revoked.
        /// </summary>
        /// <param name="fromId">The unique identifier of the entity from which the package is being revoked.</param>
        /// <param name="toId">The unique identifier of the entity to which the package was previously assigned.</param>
        /// <param name="packageId">The unique identifier of the package to revoke.</param>
        /// <param name="autenticatedServiceOwnerId">The unique identifier of the authenticated service owner performing the revocation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>It returns a result indicating whether the operation was successful or not. It returns a bool value indication if an actual package was removed or not</returns>
        Task<Result<bool>> RevokePackage(Guid fromId, Guid toId, Guid packageId, Guid autenticatedServiceOwnerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Allows service owners to delegate rights on a resource they own to an assignment connecting two entities. If the assignment does not exist, it will be created.
        /// </summary>
        /// <param name="fromId">The ID of the entity from which the resource is being delegated.</param>
        /// <param name="toId">The ID of the entity to which the resource is being delegated.</param>
        /// <param name="resource">The resource being delegated.</param>
        /// <param name="rightKeys">The right keys on the resource to delegate. All keys must exist in the resource policy.</param>
        /// <param name="authenticatedServiceOwnerId">The unique identifier of the authenticated service owner performing the delegation.</param>
        /// <param name="configureConnection">An optional action to configure connection options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A result indicating whether the operation was successful, including the details of the assignment resource if it was added successfully.</returns>
        Task<Result<AssignmentResourceDto>> AddResource(Guid fromId, Guid toId, Resource resource, IEnumerable<string> rightKeys, Guid authenticatedServiceOwnerId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Allows service owners to revoke a resource delegation from an assignment connecting two entities.
        /// Only resources delegated by the service owner itself can be revoked. If the assignment is empty after the revoke
        /// and was created by the service owner, the assignment is revoked as well.
        /// </summary>
        /// <param name="fromId">The unique identifier of the entity from which the resource is being revoked.</param>
        /// <param name="toId">The unique identifier of the entity to which the resource was previously delegated.</param>
        /// <param name="resourceId">The unique identifier of the resource to revoke.</param>
        /// <param name="authenticatedServiceOwnerId">The unique identifier of the authenticated service owner performing the revocation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A result indicating whether the operation was successful or not. The bool value indicates if an actual resource delegation was removed or not.</returns>
        Task<Result<bool>> RevokeResource(Guid fromId, Guid toId, Guid resourceId, Guid authenticatedServiceOwnerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the right keys available for delegation on a resource, as defined by the resource policy in the resource registry.
        /// </summary>
        /// <param name="resource">The resource registry identifier of the resource.</param>
        /// <param name="languageCode">The language code used for the right names.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A result with the list of rights available on the resource.</returns>
        Task<Result<List<RightDto>>> GetResourceRights(string resource, string languageCode = "nb", CancellationToken cancellationToken = default);
    }
}
