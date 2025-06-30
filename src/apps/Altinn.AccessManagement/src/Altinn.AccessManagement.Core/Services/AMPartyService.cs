using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Core.Services
{
    /// <summary>
    /// Service to handle party operations for parties in AM database
    /// </summary>
    public class AMPartyService(IAmPartyRepository ampartyRepository) : IAMPartyService
    {
        private readonly IAmPartyRepository _amPartyRepository = ampartyRepository;

        /// <inheritdoc />
        public async Task<MinimalParty> GetByOrgNo(Authorization.Api.Contracts.Register.OrganizationNumber orgNo, CancellationToken cancellationToken = default)
        {
           return await _amPartyRepository.GetByOrgNo(orgNo, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MinimalParty> GetByPartyId(int partyId, CancellationToken cancellationToken = default)
        {
            return await _amPartyRepository.GetByPartyId(partyId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MinimalParty> GetByPersonNo(PersonIdentifier personNo, CancellationToken cancellationToken = default)
        {
            return await _amPartyRepository.GetByPersonNo(personNo, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MinimalParty> GetByUid(Guid partyUuid, CancellationToken cancellationToken = default)
        {
            return await _amPartyRepository.GetByUuid(partyUuid, cancellationToken);
        }
    }
}
