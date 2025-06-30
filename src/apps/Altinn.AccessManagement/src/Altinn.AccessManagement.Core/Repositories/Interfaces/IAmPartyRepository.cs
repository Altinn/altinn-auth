using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Core.Repositories.Interfaces
{
    /// <summary>
    /// Interface for party repository
    /// </summary>
    public interface IAmPartyRepository
    {
        /// <summary>
        /// Get party by party uuid
        /// </summary>
        public Task<MinimalParty> GetByUuid(Guid partyUuid, CancellationToken cancellationToken = default);

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
    }
}
