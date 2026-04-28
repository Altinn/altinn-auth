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
    }
}
