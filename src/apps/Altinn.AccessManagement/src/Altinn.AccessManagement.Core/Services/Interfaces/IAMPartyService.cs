using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Core.Services.Interfaces
{
    /// <summary>
    /// Service for parties based on local copy of parties in AM database
    /// </summary>
    public interface IAMPartyService
    {
        /// <summary>
        /// Get party by party uuid
        /// </summary>
        public Task<MinimalParty> GetByUid(Guid partyUuid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get by party id
        /// </summary>
        public Task<MinimalParty> GetByPartyId(int partyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Party by org number
        /// </summary>
        public Task<MinimalParty> GetByOrgNo(Authorization.Api.Contracts.Register.OrganizationNumber orgNo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Party by person number
        /// </summary>
        public Task<MinimalParty> GetByPersonNo(PersonIdentifier personNo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get by user id
        /// </summary>
        public Task<MinimalParty> GetByUserId(int userId, CancellationToken cancellationToken = default);
    }
}
