using Altinn.AccessManagement.Core.Models.Authentication;
using DefaultRight = Altinn.AccessManagement.Core.Models.Authentication.DefaultRight;

namespace Altinn.AccessManagement.Core.Clients.Interfaces
{
    /// <summary>
    /// Authentication interface.
    /// </summary>
    public interface IAuthenticationClient
    {
        /// <summary>
        /// Fetching a System user from Authentication
        /// </summary>
        /// <param name="partyId">The party id of the party the systemUSer is registered on</param>
        /// <param name="systemUserId">The uuid identifying the systemUser</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<SystemUser> GetSystemUser(int partyId, string systemUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get defined rights for a given System
        /// </summary>
        /// <param name="systemId">The uuid identifier of the system</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<List<DefaultRight>> GetDefaultRightsForRegisteredSystem(string systemId, CancellationToken cancellationToken = default);
    }
}
