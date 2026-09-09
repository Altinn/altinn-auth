using Altinn.AccessManagement.Core.Models.IdPortenAuthorization;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for the ID-porten authorization service
    /// </summary>
    public interface IIdPortenAuthorizationService
    {
        /// <summary>
        /// Returns all ID-porten authorizations for the given user.
        /// </summary>
        /// <returns></returns>
        Task<Result<List<IdPortenAuthorization>>> GetIdPortenAuthorizations(Guid partyUuid, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a specific ID-porten authorization based on the id.
        /// </summary>
        /// <returns></returns>
        Task<Result<bool>> DeleteIdPortenAuthorization(Guid partyUuid, string id, CancellationToken cancellationToken);
    }
}
